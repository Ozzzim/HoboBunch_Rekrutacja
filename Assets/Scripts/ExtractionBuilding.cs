using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtractionBuilding : BuildingWithInventory
{
    public float timeToExtract = 5f;

    float timeProgress = 0f;
    public GameResourceSO resourceSO;
    //public GameResourcesList resourcesList;//Moved to BuildingWithInventory
    
    [SerializeField]
    FloatingText floatingTextPrefab;

    // Start is called before the first frame update
    void Start()
    {
        timeProgress = 0f;
        Deliveryman.NewBuildingNotify();
    }

    // Update is called once per frame
    void Update()
    {
        timeProgress += Time.deltaTime;

        if (timeProgress > timeToExtract)
        {
            Extract();
            timeProgress = 0f;
        }
    }

    private void Extract()
    {
        resourcesList.Add(resourceSO, 1);

        var floatingText = Instantiate(floatingTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        floatingText.SetText(resourceSO.resourceName + " +1");
    }

    //=====Building with Inventory methods=====//
    public override GameResource GetResource(int amount = 1)
    {
        if(resourcesList.TryUse(resourceSO,amount)){
            GameResource gameResource = new GameResource(resourceSO);
            gameResource.amount = amount;
            return gameResource;
        }
        return null;
    }
    public override GameResource GetResource(GameResourceSO grso, int amount = 1){
        if(grso == resourceSO && resourcesList.TryUse(resourceSO,amount)){
            GameResource gameResource = new GameResource(resourceSO);
            gameResource.amount = amount;
            return gameResource;
        }
        return null;
    }
    public override bool PutResource(GameResource gameResource){return false;}
    public override int GetPriority(){ return basePriority + (HasOutputResource()?1:0);}//Priority is higher if extraction site has resources ready.

    public override GameResourceSO InputResource(){ return null;}
    public override bool HasEnoughInputResource(){ return true;}

    public override GameResourceSO OutputResource(){ return resourceSO;}
    public override bool HasOutputResource(){ return resourcesList.HasResource(resourceSO);}

}
