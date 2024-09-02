using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum TicTacToeState { none, cross, circle }

[System.Serializable]
public class WinnerEvent : UnityEvent<int> { }

public class TicTacToeAI : MonoBehaviour
{
    int _aiLevel;
    TicTacToeState[,] boardState;

    [SerializeField]
    private bool _isPlayerTurn;

    [SerializeField]
    private int _gridSize = 3;

    [SerializeField]
    private TicTacToeState playerState = TicTacToeState.cross;

    [SerializeField]
    private TicTacToeState aiState = TicTacToeState.circle;

    [SerializeField]
    private GameObject _xPrefab;

    [SerializeField]
    private GameObject _oPrefab;

    public UnityEvent onGameStarted;

    public WinnerEvent onPlayerWin;

    [HideInInspector]
    public UnityEvent EnableClicksEvent;

    ClickTrigger[,] _triggers;

    private void Awake()
    {
        if (onPlayerWin == null)
        {
            onPlayerWin = new WinnerEvent();
        }

        boardState = new TicTacToeState[_gridSize, _gridSize];
    }

    public void StartAI(int AILevel)
    {
        _aiLevel = AILevel;
        StartGame();
        _isPlayerTurn = true;
    }

    public void RegisterTransform(int myCoordX, int myCoordY, ClickTrigger clickTrigger)
    {
        _triggers[myCoordX, myCoordY] = clickTrigger;
        boardState[myCoordX, myCoordY] = TicTacToeState.none;
    }

    private void StartGame()
    {
        _triggers = new ClickTrigger[_gridSize, _gridSize];
        onGameStarted.Invoke();
    }

    public void PlayerSelects(int coordX, int coordY)
    {
        EnableClicksEvent.Invoke();
        SetVisual(coordX, coordY, playerState);
        boardState[coordX, coordY] = playerState;
        if (!CheckWin(playerState, true)) // Si el jugador aun no gana, entonces le toca el turno a la IA
        {
            if (!IsBoardFull())
            {
                StartCoroutine(IATurn());
            }
            else
            {
                EndGame();
                StartCoroutine(ShowResult(-1)); // Indica un empate
            }
        }
    }

    private void SetVisual(int coordX, int coordY, TicTacToeState targetState)
    {
        Instantiate(targetState == TicTacToeState.circle ? _oPrefab : _xPrefab, _triggers[coordX, coordY].transform.position, Quaternion.identity);
    }

    public bool RegisterOccupiedCoords(int coordX, int coordY, TicTacToeState targetState) // Funcion que indica si el espacio esta ocupado con el targetstate especificado (true) o no (false)
    {
        return boardState[coordX, coordY] == targetState;
    }

    private IEnumerator IATurn()
    {
        _isPlayerTurn = false;
        yield return new WaitForSeconds(2f);

        (int x, int y) = GetBestMove();
        AiSelects(x, y);

        yield return new WaitForSeconds(1f);
        EnableClicksEvent.Invoke();
        _isPlayerTurn = true;
    }

    private (int, int) GetBestMove()
    {
        // Contabilizamos primero cuantas casillas quedan libres
        List<(int, int)> availableMoves = new List<(int, int)>();
        for (int i = 0; i < _gridSize; i++)
        {
            for (int j = 0; j < _gridSize; j++)
            {
                if (RegisterOccupiedCoords(i, j, TicTacToeState.none))
                {
                    availableMoves.Add((i, j));
                }
            }
        }

        if (availableMoves.Count == 0)
        {
            return (-1, -1); // No hay movimientos disponibles por alguna razon (solo por precaucion, es poco probable que esta parte del codigo suceda)
        }

        if (_aiLevel == 0)
        {
            // Dificultad EASY: Movimiento aleatorio
            return availableMoves[Random.Range(0, availableMoves.Count)];
        }
        else if (_aiLevel == 1)
        {
            // Dificultad HARD: Combinacion de estrategias ofensivas y defensivas

            // 1ro. Intentar ganar si es posible
            foreach (var move in availableMoves)
            {
                int x = move.Item1;
                int y = move.Item2;
                boardState[x, y] = aiState;
                if (CheckWin(aiState, false)) // Verificara ntes si la IA puede ganar
                {
                    boardState[x, y] = TicTacToeState.none;
                    return (x, y);
                }
                boardState[x, y] = TicTacToeState.none;
            }

            // 2do. Bloquear al jugador si esta a punto de ganar
            foreach (var move in availableMoves)
            {
                int x = move.Item1;
                int y = move.Item2;
                boardState[x, y] = playerState;
                if (CheckWin(playerState, false)) // Verificar antes si el jugador puede ganar
                {
                    boardState[x, y] = TicTacToeState.none;
                    return (x, y);
                }
                boardState[x, y] = TicTacToeState.none;
            }

            // 3ro. Priorizar posiciones estrategicas aleatoriamente 
            List<(int, int)> strategicMoves = new List<(int, int)>();

            // Agregar el centro si esta disponible
            if (RegisterOccupiedCoords(1, 1, TicTacToeState.none))
            {
                strategicMoves.Add((1, 1));
            }

            //Verificamos dos posibles estrategias mas

            int whilecounter = 0;

            // Agregar las esquinas si estan disponibles
            var corners = new List<(int, int)> { (0, 0), (0, 2), (2, 0), (2, 2) };
            (int, int) corner;

            do
            {
                corner = corners[Random.Range(0, corners.Count)];
                whilecounter++;
            }
            while (!RegisterOccupiedCoords(corner.Item1, corner.Item2, TicTacToeState.none) && whilecounter <= corners.Count);

            if (whilecounter <= corners.Count)
            {
                strategicMoves.Add(corner);
            }

            whilecounter = 0;

            // Agregar los bordes si estan disponibles
            var edges = new List<(int, int)> { (1, 0), (0, 1), (2, 1), (1, 2) };
            (int, int) edge;

            do
            {
                edge = edges[Random.Range(0, edges.Count)];
                whilecounter++;
            }
            while (!RegisterOccupiedCoords(edge.Item1, edge.Item2, TicTacToeState.none) && whilecounter <= edges.Count);

            if (whilecounter <= edges.Count)
            {
                strategicMoves.Add(edge);
            }

            // Elegir aleatoriamente entre las posiciones estrategicas si existen; sino, elegir cualquier movimiento random disponible

            return strategicMoves.Count > 0 ? strategicMoves[Random.Range(0, strategicMoves.Count)] : availableMoves[Random.Range(0, availableMoves.Count)];
        }

        return (-1, -1); // No hay movimientos disponibles (solo por precaucion, tampoco es probable que esta parte del codigo suceda)
    }

    public void AiSelects(int coordX, int coordY)
    {
        SetVisual(coordX, coordY, aiState);
        boardState[coordX, coordY] = aiState;
        if (!CheckWin(aiState, true)) // Si la IA aun no gana, verifica si el tablero esta lleno
        {
            if (IsBoardFull())
            {
                EndGame();
                StartCoroutine(ShowResult(-1)); // Indica un empate
            }
        }
    }

    private bool CheckWin(TicTacToeState state, bool checkFinalWin)
    //El valor booleano es usado para realizar la colocacion de la ficha o no. Esta funcion primero realiza una simulacion de un movimiento y luego revisa si hay un ganador
    {
        // Verificar filas
        for (int i = 0; i < _gridSize; i++)
        {
            if (RegisterOccupiedCoords(i, 0, state) && RegisterOccupiedCoords(i, 1, state) && RegisterOccupiedCoords(i, 2, state))
            {
                if (checkFinalWin)
                {
                    EndGame();
                    StartCoroutine(ShowResult(state == aiState ? 1 : 2));
                }
                return true;
            }
        }

        // Verificar columnas
        for (int j = 0; j < _gridSize; j++)
        {
            if (RegisterOccupiedCoords(0, j, state) && RegisterOccupiedCoords(1, j, state) && RegisterOccupiedCoords(2, j, state))
            {
                if (checkFinalWin)
                {
                    EndGame();
                    StartCoroutine(ShowResult(state == aiState ? 1 : 2));
                }
                return true;
            }
        }

        // Verificar diagonales
        if ((RegisterOccupiedCoords(0, 0, state) && RegisterOccupiedCoords(1, 1, state) && RegisterOccupiedCoords(2, 2, state)) ||
            (RegisterOccupiedCoords(0, 2, state) && RegisterOccupiedCoords(1, 1, state) && RegisterOccupiedCoords(2, 0, state)))
        {
            if (checkFinalWin)
            {
                EndGame();
                StartCoroutine(ShowResult(state == aiState ? 1 : 2));
            }
            return true;
        }

        return false;
    }

    private bool IsBoardFull()
    {
        for (int i = 0; i < _gridSize; i++)
        {
            for (int j = 0; j < _gridSize; j++)
            {
                if (RegisterOccupiedCoords(i, j, TicTacToeState.none))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void EndGame()
    {
        _isPlayerTurn = false;
        EnableClicksEvent.RemoveAllListeners(); // Elimina todos los listeners para deshabilitar los clicks
    }

    private IEnumerator ShowResult(int result)
    {
        yield return new WaitForSeconds(1f);
        onPlayerWin.Invoke(result);
    }
}
