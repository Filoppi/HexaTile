using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using UnityEditor;
using UnityEngine;

public class TilesGenerator : MonoBehaviour {

	public enum GameMode
	{
		TwoTilesPerSelection,
		OneTilePerSelection,
		OneTile
	}
	public GameObject tile;
	private GameObject[] tiles;
	private SpriteRenderer[] tilesSprites;

	public int tilesNum = 100;

	public int shapeSides = 6;

	//A different number is not supported yet
	public int rows = 10;
	private int colums = 10;

	public float tileSizeX = 1.1F;
	public float tileSizeY = 0.9F;

	//Difficulty:
	public bool timed = false;
	public float cycleTime = 1.5F;
	public int redTilesNum = 5;

	private int tilesPerRow;

	private int[] redTiles;
	private List<int> ownedTiles = new List<int>();
	private List<int> currentTiles = new List<int>();
	private List<int> selectedTiles = new List<int>();

	//Colors
	Color defaultColor = Color.white;
	Color redColor = Color.red;
	Color ownedColor = Color.black;
	Color currentColor = Color.green;
	Color selectedColor = Color.yellow;

	private int selectionIndex = 0;
	private bool selectionDirection = true; //True is Clockwise
	float elapsedTime = 0f;
	bool failed = false;

	// Use this for initialization
	void Start()
	{
		colums = rows; //Temp

		tiles = new GameObject[tilesNum];
		tilesSprites = new SpriteRenderer[tilesNum];
		for (int i = 0; i < tilesNum; i++)
		{
			tiles[i] = Instantiate(tile);
			tilesSprites[i] = tiles[i].GetComponent<SpriteRenderer>();
			tilesSprites[i].color = defaultColor;
		}

		redTiles = new int[redTilesNum];
		for (int i = 0; i < redTilesNum; i++)
		{
			redTiles[i] = Random.Range(0, tilesNum);
			for (int k = 0; k < i; k++)
			{
				if (redTiles[k] == redTiles[i]) //Don't let it be the start tile
				{
					i--;
					continue;
				}
			}
			if (redTiles[i] == tilesNum / 2 + colums / 2) //Don't let it be the start tile
			{
				i--;
			}
		}

		tilesPerRow = tilesNum / rows;

		if ((tilesNum / rows) % (float)colums == 0F)
		{
			for (int i = 0; i < rows; i++)
			{
				for (int k = 0; k < colums; k++)
				{
					//tile.GetComponent<SpriteRenderer>().sprite.texture.dimension.
					bool even = i % 2 == 0;
					tiles[(i * tilesPerRow) + k].transform.position = new Vector3(k * tileSizeX + (even ? 0F : (tileSizeX * 0.5F)), (-i * tileSizeY));

					for (int n = 0; n < redTilesNum; n++)
					{
						if (redTiles[n] == (i * tilesPerRow) + k)
						{
							tilesSprites[(i * tilesPerRow) + k].color = redColor;
						}
					}
				}
			}
		}
		else
		{
			//Error
		}

		currentTiles = new List<int> { tilesNum / 2 + colums / 2 };
		tilesSprites[tilesNum / 2 + colums / 2].color = currentColor;

		selectionIndex = (shapeSides / 4);
		selectionDirection = true;
		UpdateSelectedTiles();

		transform.position = new Vector3(tiles[tilesNum / 2 + colums / 2].transform.position.x, tiles[tilesNum / 2 + colums / 2].transform.position.y, -10);
	}

	// Update is called once per frame
	void Update()
	{
		elapsedTime += Time.deltaTime;

#if DEBUG
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			UpdateSelectionIndex(true);
			UpdateSelectedTiles();
			elapsedTime = 0;
		}
		else if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			UpdateSelectionIndex(false);
			UpdateSelectedTiles();
			elapsedTime = 0;
		}
		else
#endif
		if (timed && elapsedTime > cycleTime)
		{
			UpdateSelectionIndex(selectionDirection);
			UpdateSelectedTiles();
			elapsedTime -= cycleTime;
		}
		else if ((!timed || (elapsedTime < cycleTime - 0.218F && elapsedTime > 0.15F)) && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0)))
		{
			ConfirmSelectedTiles();
			elapsedTime = 0F;
		}

		if (failed)
		{
			for (int i = 0; i < tilesNum; i++)
			{
				tilesSprites[i].color = redColor;
			}
		}
	}

	void UpdateSelectionIndex(bool direction)
	{
		if (direction)
		{
			selectionIndex++;
			if (selectionIndex >= (shapeSides / 2) - 1)
			{
				selectionDirection = false;
				selectionIndex = (shapeSides / 2) - 1;
			}
		}
		else
		{
			selectionIndex--;
			if (selectionIndex <= 0)
			{
				selectionDirection = true;
				selectionIndex = 0;
			}
		}
	}

	void UpdateSelectedTiles()
	{
		//Restore colors of the tiles that were selected but are not anymore
		for (int i = 0; i < selectedTiles.Count; i++)
		{
			if (redTiles.Contains(selectedTiles[i]))
			{
				tilesSprites[selectedTiles[i]].color = redColor;
			}
			else if (currentTiles.Contains(selectedTiles[i]))
			{
				tilesSprites[selectedTiles[i]].color = currentColor;
			}
			else if (ownedTiles.Contains(selectedTiles[i]))
			{
				tilesSprites[selectedTiles[i]].color = ownedColor;
			}
			else
			{
				tilesSprites[selectedTiles[i]].color = defaultColor;
			}
		}

		selectedTiles.Clear();

		for (int i = 0; i < currentTiles.Count; i++)
		{
			int row = currentTiles[i] / tilesPerRow;
			int colum = currentTiles[i] / tilesPerRow;
			bool even = row % 2 == 0;
			if (selectionIndex == 0)
			{
				if (even)
				{
					selectedTiles.Add(currentTiles[i] + tilesPerRow - 1);
					selectedTiles.Add(currentTiles[i] - tilesPerRow);
				}
				else
				{
					selectedTiles.Add(currentTiles[i] + tilesPerRow);
					selectedTiles.Add(currentTiles[i] - tilesPerRow + 1);
				}
			}
			else if (selectionIndex == 1)
			{
				//Only if it fits the row
				if (colum > 0)
				{
					selectedTiles.Add(currentTiles[i] - 1);
				}
				if (colum < colums - 1)
				{
					selectedTiles.Add(currentTiles[i] + 1);
				}
			}
			else //if (selectionIndex == 2)
			{
				if (even)
				{
					selectedTiles.Add(currentTiles[i] + tilesPerRow);
					selectedTiles.Add(currentTiles[i] - tilesPerRow - 1);
				}
				else
				{
					selectedTiles.Add(currentTiles[i] + tilesPerRow + 1);
					selectedTiles.Add(currentTiles[i] - tilesPerRow);
				}
			}
		}

		for (int i = 0; i < selectedTiles.Count; i++)
		{
			if (redTiles.Contains(selectedTiles[i]))
			{
				tilesSprites[selectedTiles[i]].color = Color.Lerp(selectedColor, redColor, 0.569F);
			}
			else
			{
				tilesSprites[selectedTiles[i]].color = selectedColor;
			}
		}
	}

	void ConfirmSelectedTiles()
	{
		ownedTiles.AddRange(currentTiles);
		for (int i = 0; i < ownedTiles.Count; i++)
		{
			tilesSprites[ownedTiles[i]].color = ownedColor;
		}

		currentTiles = new List<int>(selectedTiles);
		for (int i = 0; i < currentTiles.Count; i++)
		{
			tilesSprites[currentTiles[i]].color = currentColor;
			if (redTiles.Contains(selectedTiles[i]))
			{
				failed = true;
			}
		}

		selectionIndex = (shapeSides / 4);
		selectionDirection = true;
		UpdateSelectedTiles();
	}
}