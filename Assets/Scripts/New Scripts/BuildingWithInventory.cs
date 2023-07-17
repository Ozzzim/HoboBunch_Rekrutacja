using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuildingWithInventory : MonoBehaviour
{
    /*
     * Abstract class containing interactions for the Deliveryman
     */
    [SerializeField]
    protected string displayedName;
    
    [SerializeField]
    protected int basePriority=0;
    public GameResourcesList resourcesList;
    
    [SerializeField]
    protected Transform enterance;
    [SerializeField]
    protected float minimalDistanceFromEnterance=0.2f;

    //Notify all deliverymen, so they don't end up going to nowhere
    void OnDestroy(){ Deliveryman.DestroyBuildingNotify(this);}
    public virtual Transform GetEnterance(){ return enterance;}
    
    //Is Deliveryman in interaction range with the building?
    public virtual bool CanInteract(Deliveryman d){ return Vector3.Distance(d.transform.position,enterance.position)<minimalDistanceFromEnterance;}
    //Buildings with higher priority will be handled first by the Deliverymen
    public virtual int GetPriority(){ return basePriority;}

    //===== Inventory operations =====//

    //Basic output operation
    public abstract GameResource GetResource(int amount = 1);
    //Output operation with specified resource
    public abstract GameResource GetResource(GameResourceSO grso, int amount = 1);
    //Input operation
    public abstract bool PutResource(GameResource gameResource);

    //Input checks
    //Returns resource this building needs to make the product 
    public abstract GameResourceSO InputResource();
    //Does the building have enough Input to make the produce?
    public abstract bool HasEnoughInputResource();

    //Output checks
    //Returns resource this building produces 
    public abstract GameResourceSO OutputResource();
    //Does the facility have an output product ready to pick up? This is NOT if it outputs a resource at all.
    public abstract bool HasOutputResource();
    
    //IO direction irrelevant checks
    //Is there a resource in the building's resource list?
    public virtual bool HasResource(GameResourceSO grso){ return resourcesList.HasResource(grso);}
    //Does the building have any resources at all?
    public virtual bool HasResource(){ return resourcesList.resources.Count>0;}
    public virtual string GetName(){ return displayedName;}

}
