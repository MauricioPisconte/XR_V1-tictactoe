using System;
using System.Collections.Generic;
using UnityEngine;

public class ClickTrigger : MonoBehaviour
{
	TicTacToeAI _ai;

	[SerializeField]
	private int _myCoordX = 0;
	[SerializeField]
	private int _myCoordY = 0;

	[SerializeField]
	private bool canClick;

	private void Awake()
	{
		_ai = FindObjectOfType<TicTacToeAI>();
	}

	private void Start() {

		_ai.onGameStarted.AddListener(AddReference);
		_ai.EnableClicksEvent.AddListener(AllowClick);
        _ai.onPlayerWin.AddListener((win) => SetInputEndabled(false));
        _ai.onGameStarted.AddListener(() => SetInputEndabled(true));
	}

	private void SetInputEndabled(bool val) {
		canClick = val;
	}

	private void AddReference()
	{
		_ai.RegisterTransform(_myCoordX, _myCoordY, this);
		canClick = true;
	}

	private void OnMouseDown()
	{
		if (canClick && _ai.RegisterOccupiedCoords(_myCoordX, _myCoordY, TicTacToeState.none))
		{
			_ai.PlayerSelects(_myCoordX, _myCoordY);
			//Debug.Log("coordenadas X: " + _myCoordX);
			//Debug.Log("coordenadas Y: " + _myCoordY);
		}
	}

	private void AllowClick()
	{
		canClick = !canClick;
	}
}
