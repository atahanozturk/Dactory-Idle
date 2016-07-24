﻿using UnityEngine;
using System.Collections;

public class ItemPlacer : MonoBehaviour {

	public GameObject beltPrefab;
	public GameObject[] items;
	public GameObject curItem;
	public ItemBaseScript itemScript;

	public bool isPlacingItem = false;
	public bool isMovementEnabled = true;
	public bool isBeltPlacing = false;

	public int curItemId = 0;

	Camera mycam;
	public GameObject UIBeltThingy;

	// Use this for initialization
	void Start () {
		mycam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
		if (isPlacingItem)
			PlaceItemCheck ();
		if (isBeltPlacing && !lastFrameStuff)
			PlaceBeltsCheck ();
		lastFrameStuff = false;
	}

	void PlaceItemCheck () {
		if (Input.touchCount > 0) {
			Ray myRay = mycam.ScreenPointToRay (Input.GetTouch (0).position);
			RaycastHit hit = new RaycastHit ();
			if (Physics.Raycast (myRay, out hit)) {
				TileBaseScript tileS;
				try {
					tileS = hit.collider.gameObject.GetComponent<TileBaseScript> ();
				} catch {
					return;
				}

				itemScript.x = tileS.x;
				itemScript.y = tileS.y;
				curItem.transform.position = tileS.transform.position; 
			}
		} else {
			isPlacingItem = false;
			if (curItem != null) {
				itemScript.PlaceSelf ();
				Destroy (curItem.gameObject);
				curItem = null;
				itemScript = null;
			}
		}
	}

	public void PlaceItem (int id) {
		print ("place item");
		isPlacingItem = true;
		isBeltPlacing = false;
		isMovementEnabled = true;
		UIBeltThingy.SetActive (false);
		curItemId = id;
		curItem = (GameObject)Instantiate (items[curItemId], transform.position, Quaternion.identity);
		itemScript = curItem.GetComponent<ItemBaseScript> ();
	}

	public void SelectDown () {
		isPlacingItem = false;
		isBeltPlacing = false;
		UIBeltThingy.SetActive (false);
	}

	public void SelectUp(){
		isMovementEnabled = true;
	}

	//--------------------------------------------------------------------------------------------------BELT STUFF
	bool lastFrameStuff = false;
	public void ActivateBeltMode () {
		isMovementEnabled = false;
		isBeltPlacing = true;
		UIBeltThingy.SetActive (true);
		lastFrameStuff = true;
	}

	PlacedItemBaseScript b_lastItem;
	BeltScript b_lastBelt;
	TileBaseScript b_lastTile;

	void PlaceBeltsCheck () {
		if (Input.touchCount > 0) {
			Ray myRay = mycam.ScreenPointToRay (Input.GetTouch (0).position);
			RaycastHit hit = new RaycastHit ();
			if (Physics.Raycast (myRay, out hit)) {										// cast the ray
				TileBaseScript tileS;
				try {
					tileS = hit.collider.gameObject.GetComponent<TileBaseScript> ();	//hit something
				} catch {
					return;
				}

				if (b_lastTile == tileS) {												//is it still the same tile?
					//print ("do nothing");
					return;
				}
				if (b_lastTile != null) {												//how much did we moved - if too much do shit
					if (Mathf.Abs (b_lastTile.x - tileS.x) >= 2 || Mathf.Abs (b_lastTile.y - tileS.y) >= 2 || Mathf.Abs (b_lastTile.y - tileS.y) + Mathf.Abs (b_lastTile.x - tileS.x) >= 2) {
						print ("we moved 2 blocks");
						b_lastBelt = null;
						b_lastItem = null;
						b_lastTile = null;
					}
				}

				if (tileS.beltPlaceable) {					//can we place a belt here
					if (!tileS.areThereItem) {											//there are no items here so place a belt
						BeltScript myBelt = ((GameObject)Instantiate (beltPrefab, tileS.transform.position, Quaternion.identity)).GetComponent<BeltScript> ();
						tileS.areThereItem = true;
						tileS.myItem = myBelt.gameObject;

						if (b_lastTile != null) {										//this is not the starting point - update in location
							UpdateBeltInOut (b_lastTile, tileS, myBelt, true);
						}

						if (b_lastBelt != null) {										//there was a belt before this one - update its out stuff
							UpdateBeltInOut (b_lastTile, tileS, b_lastBelt, false);

							b_lastBelt.feedingBelts [b_lastBelt.n_belt] = myBelt;
							b_lastBelt.n_belt++;
						}

						if (b_lastItem != null) {										//there was an item before us - update its out stuff
							b_lastItem.outConveyors [b_lastItem.n_out] = myBelt;
							b_lastItem.n_out++;
						}

						b_lastBelt = myBelt;
						b_lastItem = null;
						b_lastTile = tileS;

					} else {															//there is an item below us
						PlacedItemBaseScript myItem = null;
						BeltScript myBelt = null;
						myItem = tileS.myItem.GetComponent<PlacedItemBaseScript> ();
						myBelt = tileS.myItem.GetComponent<BeltScript> ();

						if (b_lastBelt == null && b_lastItem == null) {								//nothing to something
							//do nothing

						} else if (b_lastBelt == null && b_lastItem != null && myItem != null) {	//item to item
							//do nothing

						} else if (b_lastBelt == null && b_lastItem != null && myBelt != null) {	//item to belt
							b_lastItem.outConveyors [b_lastItem.n_out] = myBelt;
							b_lastItem.n_out++;
							UpdateBeltInOut (b_lastTile, tileS, myBelt, true);

						} else if (b_lastBelt != null && b_lastItem == null && myBelt != null) {	//belt to belt
							UpdateBeltInOut (b_lastTile, tileS, b_lastBelt, false);
							b_lastBelt.feedingBelts [b_lastBelt.n_belt] = myBelt;
							b_lastBelt.n_belt++;

							UpdateBeltInOut (b_lastTile, tileS, myBelt, true);

						} else if (b_lastBelt != null && b_lastItem == null && myItem != null) {	//belt to item
							UpdateBeltInOut (b_lastTile, tileS, b_lastBelt, false);
							b_lastBelt.feedingItems [b_lastBelt.n_item] = myItem;
							b_lastBelt.n_item++;

							myItem.inConveyors [myItem.n_in] = b_lastBelt;
							myItem.n_in++;

						} else {
							Debug.LogError ("weird shit happened: " + b_lastTile + " - " + tileS + " - " + b_lastBelt + " - " + b_lastItem + " - " + myBelt + " - " + myItem);
						}

						b_lastBelt = myBelt;
						b_lastItem = myItem;
						b_lastTile = tileS;
					}
				} 
			}
		} else {
			b_lastBelt = null;
			b_lastItem = null;
			b_lastTile = null;
		}
	}

	void UpdateBeltInOut(TileBaseScript lastTile, TileBaseScript thisTile, BeltScript myBelt, bool isIn){
		int x = lastTile.x;
		int xOther = thisTile.x;
		int y = lastTile.y;
		int yOther = thisTile.y;

		if (isIn) {
			if (x > xOther)      //left
				myBelt.inLocations [2] = true;
			else if (x < xOther) //right
				myBelt.inLocations [0] = true;
			else if (y > yOther) //down
				myBelt.inLocations [1] = true;
			else if (y < yOther) //up
				myBelt.inLocations [3] = true;
		} else {
			if (x > xOther)      //left
				myBelt.outLocations [0] = true;
			else if (x < xOther) //right
				myBelt.outLocations [2] = true;
			else if (y > yOther) //down
				myBelt.outLocations [3] = true;
			else if (y < yOther) //up
				myBelt.outLocations [1] = true;
		}

		myBelt.UpdateGraphic ();
	}
}