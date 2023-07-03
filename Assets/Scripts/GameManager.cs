using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject piece;

    [Header("White Piece Sprites")]
    public Sprite white_king;
    public Sprite white_queen;
    public Sprite white_bishop;
    public Sprite white_knight;
    public Sprite white_rook;
    public Sprite white_pawn;


    [Header("Black Piece Sprites")]
    public Sprite black_king;
    public Sprite black_queen;
    public Sprite black_bishop;
    public Sprite black_knight;
    public Sprite black_rook;
    public Sprite black_pawn;

    [Header("Game Pieces")]
    public List<GameObject> pieces;

    [Header("Highlight Prefabs")]
    public GameObject pieceSelectHighlight;
    public GameObject moveSelectHighlight;
    private GameObject activeHighlightObject;

    [Header("Board State")]
    public string[] boardState = new string[64];

    // dictionary of piece sprites - populated by Start()
    private Dictionary<string, Sprite> pieceSprites = new Dictionary<string, Sprite>();

    // params for board coordinates
    private double a_pos = -3.85;
    private double one_pos = -3.85;
    private double square_size = 1.1;
    private double piece_size = 3;
    private double left_x_bound;
    private double right_x_bound;
    private double top_y_bound;
    private double bottom_y_bound;

    // tracking selections/clicks
    private bool pieceSelected = false;
    private List<GameObject> highlightedMoves = new List<GameObject>();

    // game logic tracking
    private string colourToMove = "white";
    private GameObject selectedPiece;
    public GameObject lastMovedPiece;



    void Start()
    {
        // populate the dictionary of piece sprites
        pieceSprites.Add("white_king", white_king);
        pieceSprites.Add("white_queen", white_queen);
        pieceSprites.Add("white_bishop", white_bishop);
        pieceSprites.Add("white_knight", white_knight);
        pieceSprites.Add("white_rook", white_rook);
        pieceSprites.Add("white_pawn", white_pawn);

        pieceSprites.Add("black_king", black_king);
        pieceSprites.Add("black_queen", black_queen);
        pieceSprites.Add("black_bishop", black_bishop);
        pieceSprites.Add("black_knight", black_knight);
        pieceSprites.Add("black_rook", black_rook);
        pieceSprites.Add("black_pawn", black_pawn);

        // calculate bounds
        left_x_bound = a_pos - (square_size/2);
        Debug.Log(left_x_bound);
        right_x_bound = a_pos - (square_size/2) + (square_size*8);
        top_y_bound = one_pos - (square_size/2)+ (square_size*8);
        bottom_y_bound = one_pos - (square_size/2);
    
    
        Debug.Log("GameManager started");
        initBoard();
    }

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 mousePosWorld = Camera.main.ScreenToWorldPoint(mousePos);
        string position = getPositionFromCoords(mousePosWorld);
        string pieceColour = "";

        checkForWideTakes();

        // highlights cells with active players pieces
        if(!pieceSelected) {
            if(activeHighlightObject != null) {
                Destroy(activeHighlightObject);
            }
            
            // raycast down and check if collides with an object tagged with "piece"
            RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero);
            if(hit.collider != null && hit.collider.gameObject.tag == "piece") {
                if(withinBounds(mousePosWorld)) {
                    pieceColour = hit.collider.gameObject.name.Substring(0, 5);
                    if(pieceColour == colourToMove) {

                        // store reference to active piece
                        selectedPiece = hit.collider.gameObject;

                        activeHighlightObject = Instantiate(pieceSelectHighlight, getCoordsFromPosition(position), Quaternion.identity);

                        // WIDE PIECE MANAGEMENT
                        int pieceWidth = selectedPiece.GetComponent<PieceScript>().getWidth();
                        if(pieceWidth > 1) {
                            // make activeHighlightObject scale wider by multiple of pieceWidth
                            Vector3 highlightScale = activeHighlightObject.transform.localScale; 
                            highlightScale.x = highlightScale.x * pieceWidth;
                            activeHighlightObject.transform.localScale = highlightScale;

                            // make wider highlight match position of wider piece
                            activeHighlightObject.transform.position = selectedPiece.transform.position;
                            
                            

                        }

                        // if mouse clicked
                        if(Input.GetMouseButtonUp(0)) {
                            if(pieceSelected) {
                                pieceSelected = false;
                            } else {
                                pieceSelected = true;
                            }

                            // get the piece that was clicked
                            GameObject pieceClicked = hit.collider.gameObject;
                            string piecePosition = getPositionFromCoords(pieceClicked.transform.position);
                            highlightMoves(pieceClicked.name, piecePosition);

                            // disable collisions on all pieces
                            foreach(GameObject obj in pieces) {
                                obj.GetComponent<BoxCollider2D>().enabled = false;
                            }
                        }
                    }
                }
            }
        // handle unselecting piece + showing next moves
        } else if (pieceSelected) {
            RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero);
            if(hit.collider != null && hit.collider.gameObject.tag == "pieceHighlight") {
                // if mouse clicked
                if(Input.GetMouseButtonUp(0)) {
                    if(pieceSelected) {
                        pieceSelected = false;
                        foreach(GameObject obj in highlightedMoves) {
                            Destroy(obj);
                        }

                        // enable collisions on all pieces
                        foreach(GameObject obj in pieces) {
                            obj.GetComponent<BoxCollider2D>().enabled = true;
                        }
                    }
                }
            } 
            
            // handle piece move
            if (hit.collider != null && hit.collider.gameObject.tag == "moveHighlight") {
                // if mouse clicked
                if(Input.GetMouseButtonUp(0)) {
                        // re-enable collisions on all pieces
                        foreach(GameObject obj in pieces) {
                            obj.GetComponent<BoxCollider2D>().enabled = true;
                        }

                        string moveToPosition = getPositionFromCoords(mousePosWorld);
                        movePiece(selectedPiece, moveToPosition);
                }
            }
        }
    }

    void checkForWideTakes() {

        // if lastMovedPiece colliding with anything
        if(lastMovedPiece != null) {
            BoxCollider2D lastMovedPieceCollider = lastMovedPiece.GetComponent<BoxCollider2D>();
            for(int i = 0; i < pieces.Count; i++) {
                BoxCollider2D pieceCollider = pieces[i].GetComponent<BoxCollider2D>();
                if (lastMovedPieceCollider.IsTouching(pieceCollider)) {
                    Debug.Log("lastMovedPiece colliding with " + pieces[i].name);

                    Destroy(pieces[i]);
                    pieces.RemoveAt(i);

                    // GET WIDE
                    if(lastMovedPiece.GetComponent<PieceScript>().getWidth() < 8) {
                        Vector3 localScale = lastMovedPiece.transform.localScale;
                        localScale.x = localScale.x + (float)piece_size;
                        lastMovedPiece.transform.localScale = localScale;

                        lastMovedPiece.GetComponent<PieceScript>().increaseWidth();

                        // make lastMovedPiece move left by squaresize/2
                        if(piece.GetComponent<PieceScript>().getWidth() % 2 != 1) {
                            Vector3 positionVector = lastMovedPiece.transform.position;
                            positionVector.x -= (float)square_size / 2;
                            lastMovedPiece.transform.position = positionVector;
                        }
                    }

                    boundsCheckCorrection();
                    break; // ugh
                }
            }
        }
    }

    void boundsCheckCorrection() {
            // handle x bounds checking and shifting back in position
        if(lastMovedPiece != null) {
            // calculate left-most x position of lastMovedPiece
            int pieceWidth = lastMovedPiece.GetComponent<PieceScript>().getWidth();
            float left_x_edge = lastMovedPiece.transform.position.x - ((float)square_size * (float)(piece_size))/2;
            float right_x_edge = lastMovedPiece.transform.position.x + ((float)square_size * (float)(piece_size))/2;
            float tolerance = 0.2f;

            Vector3 lastMovedPiecePosition = lastMovedPiece.transform.position;

            Debug.Log("left_x_edge is " + left_x_edge);
            Debug.Log("left_x_bound is " + left_x_bound);

            if(left_x_edge < left_x_bound - tolerance) {
                Debug.Log("moving back to " + (left_x_bound + ((float)square_size * (float)piece_size)));
                lastMovedPiecePosition.x = ((float)left_x_bound + ((float)square_size * (float)piece_size));
                lastMovedPiece.transform.position = lastMovedPiecePosition;
            } 
                
            if (right_x_edge + 0.2 > right_x_bound + tolerance) {
                lastMovedPiecePosition.x = ((float)right_x_bound - ((float)square_size * (float)piece_size));
                lastMovedPiece.transform.position = lastMovedPiecePosition;
            }
        }
    }

    void initBoard()
    {
        // initialize the array of pieces
        // pieces = new GameObject[32];

        string[] pieceNames = { "rook", "knight", "bishop", "queen", "king", "bishop", "knight", "rook", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn", "rook", "knight", "bishop", "queen", "king", "bishop", "knight", "rook", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn", "pawn" };

        string[] piecePositions = { "a1", "b1", "c1", "d1", "e1", "f1", "g1", "h1", "a2", "b2", "c2", "d2", "e2", "f2", "g2", "h2", "a8", "b8", "c8", "d8", "e8", "f8", "g8", "h8", "a7", "b7", "c7", "d7", "e7", "f7", "g7", "h7" };

        for(int i = 0; i < 32; i++) {
            string colour = "white_";
            if(i >= 16) colour = "black_";

            pieces.Add(Instantiate(piece, getCoordsFromPosition(piecePositions[i]), Quaternion.identity));
            string name = colour + pieceNames[i];
            pieces[i].GetComponent<SpriteRenderer>().sprite = pieceSprites[name];
            pieces[i].name = colour + pieceNames[i] + "_" + i;
        }
    }

    public void movePiece(GameObject piece, string position)
    {
        piece.transform.position = getCoordsFromPosition(position);
        lastMovedPiece = piece;

        if(piece.GetComponent<PieceScript>().getWidth() == 1) {
            // check if piece is taking a piece
            RaycastHit2D hit = Physics2D.Raycast(piece.transform.position, Vector2.zero);
            if(hit.collider != null && hit.collider.gameObject.tag == "piece") {
                Debug.Log("1 wide, taking " + hit.collider.gameObject.name);

                // remove reference to piece from pieces
                for(int i = 0; i < pieces.Count; i++) {
                    if(pieces[i] == hit.collider.gameObject) {
                        pieces.RemoveAt(i);
                    }
                }

                // destroy the piece
                Destroy(hit.collider.gameObject);

                // GET WIDE
                Vector3 localScale = piece.transform.localScale;
                localScale.x = localScale.x + (float)piece_size;
                piece.transform.localScale = localScale;

                piece.GetComponent<PieceScript>().increaseWidth();

                // make piece move left by squaresize/2
                Vector3 positionVector = piece.transform.position;
                positionVector.x -= (float)square_size / 2;
                piece.transform.position = positionVector;

                }
        } 

        finishTurn();
    }

    public void finishTurn() {
        if(colourToMove == "white") {
            colourToMove = "black";
        } else {
            colourToMove = "white";
        }

        // remove all highlighted moves
        foreach(GameObject obj in highlightedMoves) {
            Destroy(obj);
        }

        // remove active highlight
        if(activeHighlightObject != null) {
            Destroy(activeHighlightObject);
        }

        // reset piece selected
        pieceSelected = false;

        // enable collisions on all pieces
        foreach(GameObject obj in pieces) {
            obj.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    public Vector3 getCoordsFromPosition(string position)
    {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        double x = -1;
        double y = -1;

        switch (letter)
        {
            case "a":
                x = a_pos;
                break;
            case "b":
                x = a_pos + square_size;
                break;
            case "c":
                x = a_pos + (square_size*2);
                break;
            case "d":
                x = a_pos + (square_size*3);
                break;
            case "e":
                x = a_pos + (square_size*4);
                break;
            case "f":
                x = a_pos + (square_size*5);
                break;
            case "g":
                x = a_pos + (square_size*6);
                break;
            case "h":
                x = a_pos + (square_size*7);
                break;
        }
        
        switch (number)
        {
            case "1":
                y = one_pos;
                break;
            case "2":
                y = one_pos + square_size;
                break;
            case "3":
                y = one_pos + (square_size*2);
                break;
            case "4":
                y = one_pos + (square_size*3);
                break;
            case "5":
                y = one_pos + (square_size*4);
                break;
            case "6":
                y = one_pos + (square_size*5);
                break;
            case "7":
                y = one_pos + (square_size*6);
                break;
            case "8":
                y = one_pos + (square_size*7);
                break;
        }

        return new Vector3((float)x, (float)y, -2);
    }

    public string getPositionFromCoords(Vector3 position) {
        string letter = "";
        string number = "";

        double a_pos_world = a_pos - (square_size / 2);
        double one_pos_world = one_pos - (square_size / 2);

        if(position.x >= a_pos_world && position.x < a_pos_world + square_size) letter = "a";
        else if(position.x >= a_pos_world + square_size && position.x < a_pos_world + square_size * 2) letter = "b";
        else if(position.x >= a_pos_world + square_size * 2 && position.x < a_pos_world + square_size * 3) letter = "c";
        else if(position.x >= a_pos_world + square_size * 3 && position.x < a_pos_world + square_size * 4) letter = "d";
        else if(position.x >= a_pos_world + square_size * 4 && position.x < a_pos_world + square_size * 5) letter = "e";
        else if(position.x >= a_pos_world + square_size * 5 && position.x < a_pos_world + square_size * 6) letter = "f";
        else if(position.x >= a_pos_world + square_size * 6 && position.x < a_pos_world + square_size * 7) letter = "g";
        else if(position.x >= a_pos_world + square_size * 7 && position.x < a_pos_world + square_size * 8) letter = "h";

        if(position.y >= one_pos_world && position.y < one_pos_world + square_size) number = "1";
        else if(position.y >= one_pos_world + square_size && position.y < one_pos_world + square_size * 2) number = "2";
        else if(position.y >= one_pos_world + square_size * 2 && position.y < one_pos_world + square_size * 3) number = "3";
        else if(position.y >= one_pos_world + square_size * 3 && position.y < one_pos_world + square_size * 4) number = "4";
        else if(position.y >= one_pos_world + square_size * 4 && position.y < one_pos_world + square_size * 5) number = "5";
        else if(position.y >= one_pos_world + square_size * 5 && position.y < one_pos_world + square_size * 6) number = "6";
        else if(position.y >= one_pos_world + square_size * 6 && position.y < one_pos_world + square_size * 7) number = "7";
        else if(position.y >= one_pos_world + square_size * 7 && position.y < one_pos_world + square_size * 8) number = "8";

        return letter + number;
    }

    private bool withinBounds(Vector3 position) {
        if(position.x >= left_x_bound && position.x <= right_x_bound && position.y >= bottom_y_bound && position.y <= top_y_bound) {
            return true;
        }
        else return false;
    }

    private void highlightMoves(string pieceName, string piecePosition) {
        string[] pieceNameSplit = pieceName.Split('_');
        string pieceColour = pieceNameSplit[0];
        string pieceType = pieceNameSplit[1];
        // switch on piece type
        switch (pieceType)
        {
            case "pawn":
                moveHighlighter(getPawnMoves(piecePosition, pieceColour));
                break;
            case "rook":
                moveHighlighter(getRookMoves(piecePosition, pieceColour));
                break;
            case "knight":
                moveHighlighter(getKnightMoves(piecePosition, pieceColour));
                break;
            case "bishop":
                moveHighlighter(getBishopMoves(piecePosition, pieceColour));
                break;
            case "queen":
                moveHighlighter(getQueenMoves(piecePosition, pieceColour));
                break;
            case "king":
                moveHighlighter(getKingMoves(piecePosition, pieceColour));
                break;
        }
    }

    private void moveHighlighter(List<string> moves) {
        // for each element in moves
        for(int i = 0; i < moves.Count; i++) {
            GameObject move = Instantiate(moveSelectHighlight, getCoordsFromPosition(moves[i]), Quaternion.identity);
            highlightedMoves.Add(move);
        }
    }

    private List<string> getKingMoves(string startingPosition, string pieceColour) {
        List<string> moves = new List<string>();
        string letter = startingPosition.Substring(0, 1);
        string number = startingPosition.Substring(1, 1);
        int rowNumber = int.Parse(number);

        if(getStraightUp(startingPosition, pieceColour).Count > 0) {
            moves.Add(getStraightUp(startingPosition, pieceColour)[0]);
        }

        if(getStraightDown(startingPosition, pieceColour).Count > 0) {
            moves.Add(getStraightDown(startingPosition, pieceColour)[0]);
        }

        if(getStraightLeft(startingPosition, pieceColour).Count > 0) {
            moves.Add(getStraightLeft(startingPosition, pieceColour)[0]);
        }

        if(getStraightRight(startingPosition, pieceColour).Count > 0) {
            moves.Add(getStraightRight(startingPosition, pieceColour)[0]);
        }

        if(getDiagonalUpRightAll(startingPosition, pieceColour).Count > 0) {
            moves.Add(getDiagonalUpRightAll(startingPosition, pieceColour)[0]);
        }

        if(getDiagonalUpLeftAll(startingPosition, pieceColour).Count > 0) {
            moves.Add(getDiagonalUpLeftAll(startingPosition, pieceColour)[0]);
        }

        if(getDiagonalDownLeftAll(startingPosition, pieceColour).Count > 0) {
            moves.Add(getDiagonalDownLeftAll(startingPosition, pieceColour)[0]);
        }

        if(getDiagonalDownRightAll(startingPosition, pieceColour).Count > 0) {
            moves.Add(getDiagonalDownRightAll(startingPosition, pieceColour)[0]);
        }

        return moves;
    }

    private List<string> getQueenMoves(string startingPosition, string pieceColour) {
        List<string> moves = new List<string>();
        moves.AddRange(getRookMoves(startingPosition, pieceColour));
        moves.AddRange(getBishopMoves(startingPosition, pieceColour));
        return moves;
    }

    private List<string> getPawnMoves(string startingPosition, string pieceColour) {
        string letter = startingPosition.Substring(0, 1);
        string number = startingPosition.Substring(1, 1);
        int rowNumber = int.Parse(number);
        List<string> moves = new List<string>();

        if(pieceColour == "white") {
            // if on starting row, can move 2 spaces
            if(rowNumber == 2) {
                moves.Add(letter + "3");
                moves.Add(letter + "4");
            }
            else {
                moves.Add(letter + (rowNumber + 1));
            }

            // check for blocked movement - must happen before attacks are detected or bad times
            for(int i = 0; i < moves.Count; i++) {
                if(checkForAnyPieceAtPosition(moves[i])) {
                    moves.Clear(); // pawn unique - cannot do a 2 move if something is in front of it!
                }
            }

            // get attacks
            if(letter != "a" && checkForEnemyPieceAtPosition(getDiagonalUpLeft(startingPosition), pieceColour)) {
                moves.Add(getDiagonalUpLeft(startingPosition));
            }

            if(letter != "h" && checkForEnemyPieceAtPosition(getDiagonalUpRight(startingPosition), pieceColour)) {
                moves.Add(getDiagonalUpRight(startingPosition));
            }

            // TODO: en passant
            // TODO: promotion
        }

        if(pieceColour == "black") {
            // if on starting row, can move 2 spaces
            if(rowNumber == 7) {
                moves.Add(letter + "6");
                moves.Add(letter + "5");
            }
            else {
                moves.Add(letter + (rowNumber - 1));
            }

            // check for blocked movement - must happen before attacks are detected or bad times
            for(int i = 0; i < moves.Count; i++) {
                if(checkForAnyPieceAtPosition(moves[i])) {
                    moves.RemoveAt(i);
                }
            }

            // get attacks
            if(letter != "a" && checkForEnemyPieceAtPosition(getDiagonalDownLeft(startingPosition), pieceColour)) {
                moves.Add(getDiagonalDownLeft(startingPosition));
            }

            if(letter != "h" && checkForEnemyPieceAtPosition(getDiagonalDownRight(startingPosition), pieceColour)) {
                moves.Add(getDiagonalDownRight(startingPosition));
            }

            // TODO: en passant
            // TODO: promotion
        }   
        return moves;
    }

    private List<string> getRookMoves(string startingPosition, string pieceColour) {
        string letter = startingPosition.Substring(0, 1);
        string number = startingPosition.Substring(1, 1);
        int rowNumber = int.Parse(number);
        List<string> moves = new List<string>();

        moves.AddRange(getStraightUp(startingPosition, pieceColour));
        moves.AddRange(getStraightDown(startingPosition, pieceColour));
        moves.AddRange(getStraightLeft(startingPosition, pieceColour));
        moves.AddRange(getStraightRight(startingPosition, pieceColour));

        return moves;
    }

    private List<string> getBishopMoves(string startingPosition, string pieceColour) {
        string letter = startingPosition.Substring(0, 1);
        string number = startingPosition.Substring(1, 1);
        int rowNumber = int.Parse(number);
        List<string> moves = new List<string>();

        moves.AddRange(getDiagonalUpLeftAll(startingPosition, pieceColour));
        moves.AddRange(getDiagonalUpRightAll(startingPosition, pieceColour));
        moves.AddRange(getDiagonalDownLeftAll(startingPosition, pieceColour));
        moves.AddRange(getDiagonalDownRightAll(startingPosition, pieceColour));

        return moves;
    }

    private List<string> getKnightMoves(string startingPosition, string pieceColour) {
        string letter = startingPosition.Substring(0, 1);
        string number = startingPosition.Substring(1, 1);
        int rowNumber = int.Parse(number);
        List<string> possibleMoves = new List<string>();
        List<string> moves = new List<string>();

        // up 2 then left
        possibleMoves.Add(decrementLetter(letter) + (rowNumber + 2));  

        // up 2 then right
        possibleMoves.Add(incrementLetter(letter) + (rowNumber + 2));

        // down 2 then left
        possibleMoves.Add(decrementLetter(letter) + (rowNumber - 2));

        // down 2 then right
        possibleMoves.Add(incrementLetter(letter) + (rowNumber - 2));

        // left 2 then up
        possibleMoves.Add(decrementLetter(decrementLetter(letter)) + (rowNumber + 1));

        // left 2 then down
        possibleMoves.Add(decrementLetter(decrementLetter(letter)) + (rowNumber - 1));

        // right 2 then up
        possibleMoves.Add(incrementLetter(incrementLetter(letter)) + (rowNumber + 1));

        // right 2 then down
        possibleMoves.Add(incrementLetter(incrementLetter(letter)) + (rowNumber - 1));

        // remove any moves that are off the board
        for(int i = 0; i < possibleMoves.Count; i++) {
            if(checkIfPositionOnBoard(possibleMoves[i])) {
                moves.Add(possibleMoves[i]);
            }
        }
        
        // clear possibleMoves and add all moves back in
        possibleMoves = new List<string>(moves);
        moves.Clear();

        // check if any moves are blocked by friendly pieces
        for(int i = 0; i < possibleMoves.Count; i++) {
            if(!checkForFriendlyPieceAtPosition(possibleMoves[i], pieceColour)) {
                moves.Add(possibleMoves[i]);
            }
        }

        return moves;
    }

    public bool checkIfPositionOnBoard(string move) {
        // Debug.Log("Checking if " + move + " is on the board");

        if(move.Length != 2) return false;

        string letter = move.Substring(0, 1);
        string number = move.Substring(1, 1);
        int rowNumber;
        
        try { rowNumber = int.Parse(number); } catch { return false; }

        if(letter == "a" || letter == "b" || letter == "c" || letter == "d" || letter == "e" || letter == "f" || letter == "g" || letter == "h") {
            if(rowNumber >= 1 && rowNumber <= 8) {
                return true;
            }
        }
        return false;
    }

    public bool checkForEnemyPieceAtPosition(string position, string playerColour) {
        RaycastHit2D hit = Physics2D.Raycast(getCoordsFromPosition(position), Vector2.zero);
        if(hit.collider != null && hit.collider.gameObject.name.Split('_')[0] != playerColour) {
            return true;
        }
        else return false;
    }

    public bool checkForFriendlyPieceAtPosition(string position, string playerColour) {
        RaycastHit2D hit = Physics2D.Raycast(getCoordsFromPosition(position), Vector2.zero);
        if(hit.collider != null && hit.collider.gameObject.name.Split('_')[0] == playerColour) {
            return true;
        }
        else return false;
    }

    public bool checkForAnyPieceAtPosition(string position) {
        RaycastHit2D hit = Physics2D.Raycast(getCoordsFromPosition(position), Vector2.zero);
        if(hit.collider != null) {
            return true;
        }
        else return false;
    }



    public List<string> getDiagonalUpLeftAll(string position, string playerColour) {
        List<string> moves = new List<string>();

        string currentPosition = position;

        bool movesExist = true;
        while(movesExist) {
            string nextPosition = getDiagonalUpLeft(currentPosition);
            if(nextPosition != null) {
                if(checkForAnyPieceAtPosition(nextPosition)) {
                    if(checkForEnemyPieceAtPosition(nextPosition, playerColour)) {
                        moves.Add(nextPosition);
                    }
                    movesExist = false;
                }
                else {
                    moves.Add(nextPosition);
                    currentPosition = nextPosition;
                }
            }
            else {
                movesExist = false;
            }
        }

        return moves;
    }

    public List<string> getDiagonalUpRightAll(string position, string playerColour) {
        List<string> moves = new List<string>();

        string currentPosition = position;

        bool movesExist = true;
        while(movesExist) {
            string nextPosition = getDiagonalUpRight(currentPosition);
            if(nextPosition != null) {
                if(checkForAnyPieceAtPosition(nextPosition)) {
                    if(checkForEnemyPieceAtPosition(nextPosition, playerColour)) {
                        moves.Add(nextPosition);
                    }
                    movesExist = false;
                }
                else {
                    moves.Add(nextPosition);
                    currentPosition = nextPosition;
                }
            }
            else {
                movesExist = false;
            }
        }

        return moves;
    }

    public List<string> getDiagonalDownLeftAll(string position, string playerColour) {
        List<string> moves = new List<string>();

        string currentPosition = position;

        bool movesExist = true;
        while(movesExist) {
            string nextPosition = getDiagonalDownLeft(currentPosition);
            if(nextPosition != null) {
                if(checkForAnyPieceAtPosition(nextPosition)) {
                    if(checkForEnemyPieceAtPosition(nextPosition, playerColour)) {
                        moves.Add(nextPosition);
                    }
                    movesExist = false;
                }
                else {
                    moves.Add(nextPosition);
                    currentPosition = nextPosition;
                }
            }
            else {
                movesExist = false;
            }
        }

        return moves;
    }

    public List<string> getDiagonalDownRightAll(string position, string playerColour) {
        List<string> moves = new List<string>();

        string currentPosition = position;

        bool movesExist = true;
        while(movesExist) {
            string nextPosition = getDiagonalDownRight(currentPosition);
            if(nextPosition != null) {
                if(checkForAnyPieceAtPosition(nextPosition)) {
                    if(checkForEnemyPieceAtPosition(nextPosition, playerColour)) {
                        moves.Add(nextPosition);
                    }
                    movesExist = false;
                }
                else {
                    moves.Add(nextPosition);
                    currentPosition = nextPosition;
                }
            }
            else {
                movesExist = false;
            }
        }

        return moves;
    }

    public string getDiagonalUpLeft(string position) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);
        string newLetter = "";
        string newNumber = "";

        switch (letter)
        {
            case "a":
                return null;
            case "b":
                newLetter = "a";
                break;
            case "c":
                newLetter = "b";
                break;
            case "d":
                newLetter = "c";
                break;
            case "e":
                newLetter = "d";
                break;
            case "f":
                newLetter = "e";
                break;
            case "g":
                newLetter = "f";
                break;
            case "h":
                newLetter = "g";
                break;
        }

        switch (rowNumber)
        {
            case 1:
                newNumber = "2";
                break;
            case 2:
                newNumber = "3";
                break;
            case 3:
                newNumber = "4";
                break;
            case 4:
                newNumber = "5";
                break;
            case 5:
                newNumber = "6";
                break;
            case 6:
                newNumber = "7";
                break;
            case 7:
                newNumber = "8";
                break;
            case 8:
                return null;
        }

        return newLetter + newNumber;
    }

    public string getDiagonalUpRight(string position) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);
        string newLetter = "";
        string newNumber = "";

        switch (letter)
        {
            case "a":
                newLetter = "b";
                break;
            case "b":
                newLetter = "c";
                break;
            case "c":
                newLetter = "d";
                break;
            case "d":
                newLetter = "e";
                break;
            case "e":
                newLetter = "f";
                break;
            case "f":
                newLetter = "g";
                break;
            case "g":
                newLetter = "h";
                break;
            case "h":
                return null;
        }

        switch (rowNumber)
        {
            case 1:
                newNumber = "2";
                break;
            case 2:
                newNumber = "3";
                break;
            case 3:
                newNumber = "4";
                break;
            case 4:
                newNumber = "5";
                break;
            case 5:
                newNumber = "6";
                break;
            case 6:
                newNumber = "7";
                break;
            case 7:
                newNumber = "8";
                break;
            case 8:
                return null;
        }

        return newLetter + newNumber;
    }

    public string getDiagonalDownRight(string position) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);
        string newLetter = "";
        string newNumber = "";

        switch (letter)
        {
            case "a":
                newLetter = "b";
                break;
            case "b":
                newLetter = "c";
                break;
            case "c":
                newLetter = "d";
                break;
            case "d":
                newLetter = "e";
                break;
            case "e":
                newLetter = "f";
                break;
            case "f":
                newLetter = "g";
                break;
            case "g":
                newLetter = "h";
                break;
            case "h":
                return null;
        }

        switch (rowNumber)
        {
            case 8:
                newNumber = "7";
                break;
            case 7:
                newNumber = "6";
                break;
            case 6:
                newNumber = "5";
                break;
            case 5:
                newNumber = "4";
                break;
            case 4:
                newNumber = "3";
                break;
            case 3:
                newNumber = "2";
                break;
            case 2:
                newNumber = "1";
                break;
            case 1:
                return null;
        }

        return newLetter + newNumber;
    }

    public string getDiagonalDownLeft(string position) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);
        string newLetter = "";
        string newNumber = "";

        switch (letter)
        {
            case "a":
                return null;    
            case "b":
                newLetter = "a";
                break;
            case "c":
                newLetter = "b";
                break;
            case "d":
                newLetter = "c";
                break;
            case "e":
                newLetter = "d";
                break;
            case "f":
                newLetter = "e";
                break;
            case "g":
                newLetter = "f";
                break;
            case "h":
                newLetter = "g";
                break;
        }

        switch (rowNumber)
        {
            case 8:
                newNumber = "7";
                break;
            case 7:
                newNumber = "6";
                break;
            case 6:
                newNumber = "5";
                break;
            case 5:
                newNumber = "4";
                break;
            case 4:
                newNumber = "3";
                break;
            case 3:
                newNumber = "2";
                break;
            case 2:
                newNumber = "1";
                break;
            case 1:
                return null;
        }

        return newLetter + newNumber;
    }

    public List<string> getStraightUp(string position, string playerColour) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);

        List<string> positions = new List<string>();

        int currentNumber = rowNumber;
        while(currentNumber < 8) {
            currentNumber = currentNumber + 1;
            if (currentNumber != 9) {
                string evaluatedPosition =  letter + currentNumber;

                // check if there is a friendly piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) == playerColour) {
                    currentNumber = 100;
                }  

                // check if there is an enemy piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) != playerColour) {     
                    positions.Add(evaluatedPosition);
                    currentNumber = 100;
                } 
                
                if (getPieceAtPosition(evaluatedPosition) == null) {
                    positions.Add(evaluatedPosition);
                }
            }
        }
        return positions;
    }

    public List<string> getStraightDown(string position, string playerColour) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);

        List<string> positions = new List<string>();

        int currentNumber = rowNumber;
        while(currentNumber > 0) {
            currentNumber = currentNumber - 1;
            if (currentNumber != 0) {
                string evaluatedPosition =  letter + currentNumber;

                // check if there is a friendly piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) == playerColour) {
                    currentNumber = -100;
                }  

                // check if there is an enemy piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) != playerColour) {     
                    positions.Add(evaluatedPosition);
                    currentNumber = -100;
                } 
                
                if (getPieceAtPosition(evaluatedPosition) == null) {
                    positions.Add(evaluatedPosition);
                }
            }
        }
        return positions;
    }

    public List<string> getStraightLeft(string position, string playerColour) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);

        List<string> positions = new List<string>();

        string currentLetter = letter;
        while (currentLetter != null) {
            currentLetter = decrementLetter(currentLetter);
            if (currentLetter != null) {
                string evaluatedPosition =  currentLetter + number;

                // check if there is a friendly piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) == playerColour) {
                    currentLetter = null;
                }  

                // check if there is an enemy piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) != playerColour) {     
                    positions.Add(evaluatedPosition);
                    currentLetter = null;
                } 
                
                if (getPieceAtPosition(evaluatedPosition) == null) {
                    positions.Add(evaluatedPosition);
                }
            }
        }
        return positions;
    }

    public List<string> getStraightRight(string position, string playerColour) {
        string letter = position.Substring(0, 1);
        string number = position.Substring(1, 1);
        int rowNumber = int.Parse(number);

        List<string> positions = new List<string>();

        string currentLetter = letter;
        while (currentLetter != null) {
            currentLetter = incrementLetter(currentLetter);
            if (currentLetter != null) {
                string evaluatedPosition =  currentLetter + number;

                // check if there is a friendly piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) == playerColour) {
                    currentLetter = null;
                }  

                // check if there is an enemy piece in the way
                if (getPieceAtPosition(evaluatedPosition) != null && getPieceColourAtPosition(evaluatedPosition) != playerColour) {     
                    positions.Add(evaluatedPosition);
                    currentLetter = null;
                } 
                
                if (getPieceAtPosition(evaluatedPosition) == null) {
                    positions.Add(evaluatedPosition);
                }
            }
        }
        return positions;
    }

    public GameObject getPieceAtPosition(string position) {
        foreach (GameObject piece in pieces) {
            if (getPositionFromCoords(piece.transform.position) == position) {
                return piece;
            }
        }
        return null;
    }

    public string getPieceColourAtPosition(string position) {
        foreach (GameObject piece in pieces) {
            if (getPositionFromCoords(piece.transform.position) == position) {
                return piece.GetComponent<PieceScript>().colour;
            }
        }
        return null;
    }

    public string incrementLetter(string startingLetter) {
        switch (startingLetter)
        {
            case "a":
                return "b";
            case "b":
                return "c";
            case "c":
                return "d";
            case "d":
                return "e";
            case "e":
                return "f";
            case "f":
                return "g";
            case "g":
                return "h";
            case "h":
                return null;
            default:
                return null;
        }
    }

    public string decrementLetter(string startingLetter) {
        switch (startingLetter)
        {
            case "h":
                return "g";
            case "g":
                return "f";
            case "f":
                return "e";
            case "e":
                return "d";
            case "d":
                return "c";
            case "c":
                return "b";
            case "b":
                return "a";
            case "a":
                return null;
            default:
                return null;
        }
    }

}
