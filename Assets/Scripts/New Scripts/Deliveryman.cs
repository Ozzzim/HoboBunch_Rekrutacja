using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

public class Deliveryman : MonoBehaviour
{
    protected NavMeshAgent navMeshAgent;
    protected Animator animator;

    public GameResource inventory;
    protected BuildingWithInventory destination;
    private int capacity = 1;//How much can postman carry
    public int workRadius = 20;//How far from assigned Warehouse can deliveryman work
    public GameResourceSO needs;//Argument when picking items from the warehouse
    protected bool waiting = true;//Flag to prevent NewBuildingNotify() from prematurely activating the deliveryman

    [SerializeField]
    StaticText staticTextPrefab;
    [SerializeField]
    StorageBuilding attachedWarehouse;
    [SerializeField]
    GameObject resourceBox;//Crate that is used to show, that deliveryman is carrying resources

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        resourceBox.SetActive(inventory!=null);

        StartCoroutine(WaitAndPickNewDestination());//Is this a good idea?
    }

    // Update is called once per frame
    void Update()
    {
        //Animation
        animator.SetBool("HasResources", inventory!=null);
        animator.SetBool("Walking", navMeshAgent.velocity.magnitude>0.1f);

        //if(Input.GetKeyDown(KeyCode.X)){SetDestination(GetLocalBuildings()[1]);}//Debug, remove later

        if(destination && destination.CanInteract(this)){
            BuildingInteraction(destination);
        } 
    }

    //String describing current activity of the deliveryman
    public string GetThoughts(){

        if(inventory!=null){
            if(destination)
                return "Moving "+inventory.amount+" "+inventory.resourceSO.name+" to the "+destination.GetName();
            else
                return "Carrying "+inventory.resourceSO.name;
        }
        if(destination)
            return "Heading to the "+destination.GetName();
        return "Thinking about the meaning of life";
    }

    //Display deliveryman current status
    public void OnMouseDown(){
        var floatingText = Instantiate(staticTextPrefab, transform.position + Vector3.up*2, Quaternion.identity, transform);
        floatingText.SetText(GetThoughts());
    }

    //Set movement destination of the target. Returns false if given building is null;
    public bool SetDestination(BuildingWithInventory bwi){
        if(bwi){
            navMeshAgent.SetDestination(bwi.GetEnterance().position);
            destination = bwi;
            return true;
        }
        destination = null;
        return false;
    }

    //Displays range of the warehouse
    void OnDrawGizmos(){
        Gizmos.color = Color.blue;
        if(attachedWarehouse)
            Gizmos.DrawWireSphere(attachedWarehouse.transform.position,workRadius);
    }

    //Coroutine played on creation of the warehouse
    protected IEnumerator WaitAndPickNewDestination(float waitTime=1f){
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        SetOptimalDestination();
        waiting = false;
    }

    //Called after placing a new building. If any deliverymen are idle and can do anything with the new building, they will activate.
    public static void NewBuildingNotify(){
        Deliveryman[] deliverymen = FindObjectsByType<Deliveryman>(FindObjectsSortMode.None);
        foreach(Deliveryman d in deliverymen){
            if(!d.destination && !d.waiting)//If Deliveryman isn't already occupied
                d.SetOptimalDestination();
        }
    }
    //Called after destruction of a building
    public static void DestroyBuildingNotify(BuildingWithInventory biw){
        Deliveryman[] deliverymen = FindObjectsByType<Deliveryman>(FindObjectsSortMode.None);
        foreach(Deliveryman d in deliverymen){
            if(d.destination && d.destination == biw)
                d.SetOptimalDestination();
        }
    }

    //Interacts with a building and set a new destination afterward
    protected bool BuildingInteraction(BuildingWithInventory building, GameResourceSO neededResource=null){
        destination = null;//Nulled so Update() won't call BuildingInteraction again
        if(inventory == null){//Output cases
                if(building == attachedWarehouse){//When using the warehouse
                    if(needs && building.HasResource(needs)){//If deliveryman knows what he needs and warehouse has the resources.
                        inventory = building.GetResource(needs,capacity);//Pick up resources
                        resourceBox.SetActive(true);
                        if(!SetDestination(GetMostImportantBuilding(GetBuildingsNeedingResource(inventory.resourceSO))))//If no building needs the resource, put it back in the warehouse
                            SetDestination(attachedWarehouse);
                        needs=null;
                    } else //If warehouse doesn't have the resources
                        SetOptimalDestination();//Begin again
                } else { //When using buildings with specified inputs/outputs
                    if(building.OutputResource() && building.HasOutputResource()){//Check if building outputs a resource and if its ready to do so 
                        inventory = building.GetResource(capacity);//Pick up resources

                        resourceBox.SetActive(true);
                        if(!SetDestination(GetMostImportantBuilding(GetBuildingsNeedingResource(inventory.resourceSO))))//If no building needs the resource, put it back in the warehouse
                            SetDestination(attachedWarehouse);
                    } else {
                        //Why can't the output be extracted yet?
                        if(building.HasEnoughInputResource())//Building is still producing the resource
                            StartCoroutine(WaitForTheOutput(building));
                        else{//Building lost it's material while deliveryman was moving. Unlikely scenario, but ought to be covered.
                            //Check if warehouse has necessary supplies first
                            if(attachedWarehouse.HasResource(building.InputResource())){
                                SetDestination(attachedWarehouse);
                                needs = building.InputResource();
                            } else {
                                needs = null;
                                if(!SetDestination(GetBuildingMakingResource(building.InputResource())))//Find a building that produces the resource
                                    ReturnToWareHouse();//If nothing produces the necessary resource, return to the warehouse and wait for a new building to appear
                            }
                        }
                    }
                }          
        } else {//Input
            if((building.InputResource()==inventory.resourceSO || building == attachedWarehouse) && building.PutResource(inventory)){//Puts resource in as long as building has a matching input or is a warehouse
                inventory = null;
                resourceBox.SetActive(false);
                SetOptimalDestination();//Begin again
            } else 
                SetDestination(attachedWarehouse);//In a weird case where deliveryman delivered item to a facility that doesn't need it, return it to the warehouse
        }
        return true;
    }

    //Wait until building produces a new output 
    protected IEnumerator WaitForTheOutput(BuildingWithInventory building, float waitTime=3f){
        waiting = true;
        yield return new WaitUntil(building.HasOutputResource);
        SetDestination(building);
        waiting = false;
    }

    //Used in cases when deliveryman gets stuck
    public void ReturnToWareHouse(){
        navMeshAgent.SetDestination(attachedWarehouse.GetEnterance().position);
        destination=null;
    }

    //===== Destination finders =====//

    //Gets prioritized building and goes down it's production chain if it cannot produce yet
    protected void SetOptimalDestination(short loopingSafeguard=10){//Set looping safeguard to 0< for no safeguards
        BuildingWithInventory bwi = GetMostImportantBuilding(GetLocalBuildings());
        if(!bwi)//If there are no other buildings to visit
            return;
        while(true){
            
            if(bwi.HasEnoughInputResource()){
                SetDestination(bwi);
                return;
            } else{
                needs = bwi.InputResource();
                bwi = GetBuildingMakingResource(bwi.InputResource());
                if(!bwi){
                    SetDestination(attachedWarehouse);
                    return;
                }
            }
            /*
            //Anti infinite looping. Not necessary as long as there's no looping resource chains
            if(0<loopingSafeguard){
                loopingSafeguard--;
                if(loopingSafeguard==0){
                    SetDestination(attachedWarehouse);
                    return;
                }
            }
            //*/
        }
    }

    //Returns array of all buildings that use the specified resource
    protected BuildingWithInventory[] GetBuildingsNeedingResource(GameResourceSO grso){
        BuildingWithInventory[] bwiList = GetLocalBuildings();
        
        //Find all Buildings that need the grso resource
        bwiList = Array.FindAll(bwiList,(x) => x.InputResource() == grso);
        return bwiList;
    }

    //Returns closest building that uses the specified resource
    protected BuildingWithInventory GetBuildingNeedingResource(GameResourceSO grso){
        BuildingWithInventory[] bwiList = GetLocalBuildings();
        
        //Find all Buildings that need the grso resource
        bwiList = Array.FindAll(bwiList,(x) => x.InputResource() == grso);

        //Get the closest Building
        BuildingWithInventory closest=null;
        float minDistance=0;
        float curDistance;
        foreach(BuildingWithInventory bwi in bwiList){
            curDistance = Vector3.Distance(bwi.transform.position,transform.position);
            if(closest==null || minDistance>curDistance){
                minDistance = curDistance;
                closest = bwi;
            }
        }

        return closest;
    }

    //Returns closest building that makes the specified resource
    protected BuildingWithInventory GetBuildingMakingResource(GameResourceSO grso){
        if(attachedWarehouse.HasResource(grso)){//This fukin line
            return attachedWarehouse;
        }
        BuildingWithInventory[] bwiList = GetLocalBuildings();

        //Find all Buildings that need the grso resource
        bwiList = Array.FindAll(bwiList,(x) => x.OutputResource() == grso);

        //Get the closest Building
        BuildingWithInventory closest=null;
        float minDistance=0;
        float curDistance;
        foreach(BuildingWithInventory bwi in bwiList){
            curDistance = Vector3.Distance(bwi.transform.position,transform.position);
            if(closest==null || minDistance>curDistance){
                minDistance = curDistance;
                closest = bwi;
            }
        }

        return closest;
    }

    //Returns an array of all buildings within the warehouse sphere of influence
    protected BuildingWithInventory[] GetLocalBuildings(){
        Collider[] localColliders = Physics.OverlapSphere(attachedWarehouse.transform.position, workRadius, LayerMask.GetMask("ProducerBuilding"));

        //Extract BuildingWithInventory from Collider data
        BuildingWithInventory[] localBuildings = new BuildingWithInventory[localColliders.Length];
        for(int i=0;i<localColliders.Length;i++){
            localBuildings[i] = localColliders[i].GetComponent<BuildingWithInventory>();
        }
        //Array.Sort(localBuildings,((x,y)=> x.GetPriority()-y.GetPriority()));

        return localBuildings;
    }

    //Returns building with the highest priority
    protected BuildingWithInventory GetMostImportantBuilding(BuildingWithInventory[] buildings){
        BuildingWithInventory returned=null;
        foreach(BuildingWithInventory bwi in buildings){
            if(returned==null || returned.GetPriority()<=bwi.GetPriority())
                /*//Random chance to change the target if both have the same priority
                if(returned.GetPriority()!=bwi.GetPriority())
                    returned = bwi;
                else
                    returned = (UnityEngine.Random.Range(0,1)>0.5f ? bwi : returned);
                //*/
                returned=bwi;
        }
        return returned;
    }

}

