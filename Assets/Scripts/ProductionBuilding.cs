using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : BuildingWithInventory
{
    public int inputAmountRequired = 2;
    public GameResourceSO inputResourceSO;
    public GameResourceSO outputResourceSO;

    public float timeToExtract = 5f;

    float timeProgress = 0f;
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
            Product();
            timeProgress = 0f;
        }
    }

    private void Product()
    {
        if (resourcesList.TryUse(inputResourceSO, inputAmountRequired))
        {
            resourcesList.Add(outputResourceSO, 1);

            var floatingText = Instantiate(floatingTextPrefab, transform.position + Vector3.up, Quaternion.identity);
            floatingText.SetText($"{inputResourceSO.resourceName} -{inputAmountRequired}\n{outputResourceSO.resourceName}+1");
        }
    }

    //=====Building with Inventory methods=====//
    public override GameResource GetResource(int amount = 1)
    {
        if(resourcesList.TryUse(outputResourceSO,amount)){
            GameResource gameResource = new GameResource(outputResourceSO);
            gameResource.amount = amount;
            return gameResource;
        }
        return null;
    }
    public override GameResource GetResource(GameResourceSO grso, int amount = 1){
        if(grso == outputResourceSO && resourcesList.TryUse(outputResourceSO,amount)){
            GameResource gameResource = new GameResource(outputResourceSO);
            gameResource.amount = amount;
            return gameResource;
        }
        return null;
    }

    public override bool HasResource(){
        return resourcesList.HasResource(outputResourceSO);
    }
    public override bool PutResource(GameResource gameResource)
    {
        if(gameResource.resourceSO == inputResourceSO && gameResource.amount > 0){
            resourcesList.Add(gameResource.resourceSO,gameResource.amount);
            return true;
        }
        return false;
    }

    public override GameResourceSO InputResource(){ return inputResourceSO;}
    public override GameResourceSO OutputResource(){ return outputResourceSO;}
    public override bool HasOutputResource(){ return resourcesList.HasResource(outputResourceSO);}
    public override int GetPriority(){
        int input = GetInputAmount();
        return basePriority + (input >= inputAmountRequired? 0 : 2*(inputAmountRequired-input));//Priority increases if it has not enough material
    }
    public override bool HasEnoughInputResource(){ return resourcesList.HasResource(inputResourceSO,inputAmountRequired-1);}//-1 Because has resource uses amount > inputAmountRequired, not >=
    public int GetInputAmount(){ return resourcesList.resources.Find((x) => x.resourceSO == inputResourceSO).amount;}
    

}
