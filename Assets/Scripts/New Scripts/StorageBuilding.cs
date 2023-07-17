using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageBuilding : BuildingWithInventory
{

    [SerializeField]
    FloatingText floatingTextPrefab;

    //=====Building with Inventory methods=====//
    public override GameResource GetResource(int amount = 1)
    {
        return null;
    }
    public override GameResource GetResource(GameResourceSO grso, int amount = 1)
    {
        if(resourcesList.TryUse(grso,amount)){
            GameResource gameResource = new GameResource(grso);
            gameResource.amount = amount;
            return gameResource;
        }
        return null;
    }

    public override bool PutResource(GameResource gameResource)
    {
        if(gameResource.amount > 0){
            resourcesList.Add(gameResource.resourceSO,gameResource.amount);
            return true;
        }
        return false;
    }
    public override bool HasEnoughInputResource(){ return true;}
    public override bool HasOutputResource(){ return false;}
    public override GameResourceSO InputResource(){ return null;}
    public override GameResourceSO OutputResource(){ return null;}
}
