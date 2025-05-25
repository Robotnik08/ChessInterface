using Raylib_cs;
using System.Numerics;
using System.Diagnostics;

class Program {
    static Color light_square_color = new(0xff, 0xe6, 0xcc);
    static Color dark_square_color = new(0x80, 0x42, 0x1c);

    static readonly int board_size = 1000;

    static int SquareSize => board_size / 8;

    static readonly string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    static readonly string enginePath = "engine/chess.exe";

    static readonly Dictionary<string, string> pieceImages = new()
    {
        { "K", "assets/wk.png" },
        { "Q", "assets/wq.png" },
        { "R", "assets/wr.png" },
        { "B", "assets/wb.png" },
        { "N", "assets/wn.png" },
        { "P", "assets/wp.png" },
        { "k", "assets/bk.png" },
        { "q", "assets/bq.png" },
        { "r", "assets/br.png" },
        { "b", "assets/bb.png" },
        { "n", "assets/bn.png" },
        { "p", "assets/bp.png" }
    };

    static void Main() {
        BoardState boardState = new(startFEN);

        int holdIndex = -1;
        bool isDragging = false;

        char promotionPiece = 'q'; // Default promotion piece, can be changed later

        List<string> moves = [];
        List<string> moveHistory = [];

        UpdateBoardState();

        Raylib.InitWindow(board_size, board_size, "Chess");

        Dictionary<string, Texture2D> pieceSprites = [];
        foreach (var piece in pieceImages) {
            Texture2D texture = Raylib.LoadTexture(piece.Value);
            pieceSprites.Add(piece.Key, texture);
        }

        while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();

            // Make a move list of moves to render based on the start of the move (the dragging piece)
            List<string> moveRenderList = [];
            foreach (string move in moves) {
                if (move.StartsWith($"{(char)('a' + holdIndex % 8)}{(char)('1' + (holdIndex / 8))}")) {
                    moveRenderList.Add(move);
                }
            }

            for (int file = 0; file < 8; file++) {
                for (int rank = 0; rank < 8; rank++) {
                    int draw_rank = 7 - rank; // Flip the rank for drawing

                    // If a move starts with the square, draw a red square
                    Raylib.DrawRectangle(file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, (file + rank) % 2 == 0 ? light_square_color : dark_square_color);

                    foreach (string move in moveRenderList) {
                        if (move[2] == (char)('a' + file) && move[3] == (char)('1' + rank)) {
                            Raylib.DrawRectangle(file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, new Color(255, 0, 0, 255 / 2)); // Highlight valid moves in red
                            break;
                        }
                    }
                    if (holdIndex == (rank * 8 + file) && isDragging) {
                        Raylib.DrawRectangle(file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, new Color(255, 255, 0, 255 / 2)); // Highlight the square being dragged in yellow
                    }

                    // Draw the pieces
                    char piece = boardState.board[rank][file];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString()) && !(holdIndex == (rank * 8 + file) && isDragging)) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize),
                            Vector2.Zero,
                            0,
                            Color.White
                        );
                    }
                }
            }

            if (isDragging && holdIndex != -1) {
                Vector2 mousePos = Raylib.GetMousePosition();

                if (holdIndex >= 0) {
                    int x = holdIndex % 8;
                    int y = holdIndex / 8;

                    char piece = boardState.board[y][x];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString())) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(mousePos.X - SquareSize / 2, mousePos.Y - SquareSize / 2, SquareSize, SquareSize),
                            Vector2.Zero,
                            0,
                            Color.White
                        );
                    }
                }
            }

            Raylib.EndDrawing();


            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                Vector2 mousePos = Raylib.GetMousePosition();
                int x = (int)(mousePos.X / SquareSize);
                int y = 7 - (int)(mousePos.Y / SquareSize);

                if (x >= 0 && x < 8 && y >= 0 && y < 8) {
                    char piece = boardState.board[y][x];
                    if (piece != '.') {
                        holdIndex = y * 8 + x;
                        isDragging = true;
                    }
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left) && isDragging) {
                Vector2 mousePos = Raylib.GetMousePosition();

                // check if mouse on the board
                if (mousePos.X < 0 || mousePos.X > board_size || mousePos.Y < 0 || mousePos.Y > board_size) {
                    holdIndex = -1;
                    isDragging = false;
                    continue;
                }

                int x = (int)(mousePos.X / SquareSize);
                int y = 7 - (int)(mousePos.Y / SquareSize);

                // if they are the same square, just ignore
                if (holdIndex == y * 8 + x) {
                    holdIndex = -1;
                    isDragging = false;
                    continue;
                }

                if (x >= 0 && x < 8 && y >= 0 && y < 8) {
                    char piece = boardState.board[holdIndex / 8][holdIndex % 8];
                    boardState.board[holdIndex / 8] = boardState.board[holdIndex / 8].Remove(holdIndex % 8, 1).Insert(holdIndex % 8, ".");
                    boardState.board[y] = boardState.board[y].Remove(x, 1).Insert(x, piece.ToString());

                    string generatedMove = $"{(char)('a' + holdIndex % 8)}{(char)('1' + (holdIndex / 8))}{(char)('a' + x)}{(char)('1' + y)}";
                    // Add move from move list if that's in there, otherwise generate it here
                    if (moves.Count > 0 && moves.Any(m => m.StartsWith(generatedMove))) {
                        // check if amount of moves is more than 1, if so, that means it's a promotion and it needs to pick the one based on preference
                        if (moves.Count(m => m.StartsWith(generatedMove)) > 1) {
                            moveHistory.Add(generatedMove + promotionPiece);
                        } else {
                            // find the move that matches the holdIndex
                            string move = moves.First(m => m.StartsWith(generatedMove));
                            moveHistory.Add(move);
                        }
                    } else {
                        // Generate the move in standard notation
                        moveHistory.Add(generatedMove);
                    }

                    UpdateBoardState();
                }

                promotionPiece = 'q'; // Reset promotion piece to queen after a move
                holdIndex = -1;
                isDragging = false;
            }


            if (Raylib.IsKeyPressed(KeyboardKey.F)) {
                string fen = boardState.ToFEN();
                Console.WriteLine(fen);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.C)) {
                // Clear the board to the starting position
                boardState.FromFEN(startFEN);
                moveHistory.Clear();
                UpdateBoardState();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.U)) {
                // Undo the last move
                if (moveHistory.Count > 0) {
                    moveHistory.RemoveAt(moveHistory.Count - 1);
                    UpdateBoardState();
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Q)) {
                promotionPiece = 'q'; // Queen
            }
            if (Raylib.IsKeyPressed(KeyboardKey.R)) {
                promotionPiece = 'r'; // Rook
            }
            if (Raylib.IsKeyPressed(KeyboardKey.B)) {
                promotionPiece = 'b'; // Bishop
            }
            if (Raylib.IsKeyPressed(KeyboardKey.N)) {
                promotionPiece = 'n'; // Knight
            }
        }

        Raylib.CloseWindow();

        void UpdateBoardState() {
            // Run engine/chess.exe and ask for legal moves and draw them
            Console.WriteLine("Starting engine process...");
            Process engineProcess = new();
            engineProcess.StartInfo.FileName = enginePath;
            engineProcess.StartInfo.Arguments = "engine";
            engineProcess.StartInfo.UseShellExecute = false;
            engineProcess.StartInfo.RedirectStandardInput = true;
            engineProcess.StartInfo.RedirectStandardOutput = true;
            engineProcess.StartInfo.CreateNoWindow = true;
            engineProcess.Start();
            Console.WriteLine("Engine process started.");

            StreamWriter engineInput = engineProcess.StandardInput;
            StreamReader engineOutput = engineProcess.StandardOutput;

            Console.WriteLine("Sending startFEN to engine...");
            engineInput.WriteLine("setfen");
            engineInput.WriteLine(startFEN);
            Console.WriteLine("FEN set successfully.");
            // check if response is ok
            string response = engineOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting FEN: {response}");
                return;
            }

            // Sending move history to the engine
            Console.WriteLine("Sending moves to engine... History: " + string.Join(", ", moveHistory));
            string movesString = string.Join(" ", moveHistory);
            engineInput.WriteLine("setmovehistory");
            engineInput.WriteLine(movesString);
            // check if response is ok
            response = engineOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting move history: {response}");
                return;
            }

            Console.WriteLine("Fetching Moves...");
            engineInput.WriteLine("getmoves");

            Console.WriteLine("Waiting for engine output...");
            moves = [];
            while (true) {
                string line = engineOutput.ReadLine();
                if (line == null || line == "ok") {
                    break;
                }
                moves.Add(line);
            }

            // Get FEN from the engine
            Console.WriteLine("Fetching FEN from engine...");
            engineInput.WriteLine("getfen");
            string fen = engineOutput.ReadLine();
            Console.WriteLine($"Received FEN: {fen}");
            boardState.FromFEN(fen);
            // check if response is ok
            if (fen == null || fen == "ok") {
                Console.WriteLine($"Error getting FEN: {fen}");
                return;
            }

            engineInput.WriteLine("end");
            Console.WriteLine("Moves fetched successfully.");
        }
    }
}


class BoardState {
    public List<string> board;
    public bool white_to_move;
    public bool castling_rights_white_king_side;
    public bool castling_rights_white_queen_side;
    public bool castling_rights_black_king_side;
    public bool castling_rights_black_queen_side;
    public int en_passant_target_square;
    public int halfmove_clock;
    public int fullmove_number;

    public string ToFEN() {
        // Flip the board list
        board.Reverse();

        string fen = "";

        // Convert the board to FEN format
        for (int i = 0; i < 8; i++) {
            int emptyCount = 0;
            for (int j = 0; j < 8; j++) {
                char piece = board[i][j];
                if (piece == '.') {
                    emptyCount++;
                } else {
                    if (emptyCount > 0) {
                        fen += emptyCount.ToString();
                        emptyCount = 0;
                    }
                    fen += piece;
                }
            }
            if (emptyCount > 0) {
                fen += emptyCount.ToString();
            }
            if (i < 7) {
                fen += "/";
            }
        }
        fen += " " + (white_to_move ? "w" : "b") + " ";
        fen += (castling_rights_white_king_side ? "K" : "") +
               (castling_rights_white_queen_side ? "Q" : "") +
               (castling_rights_black_king_side ? "k" : "") +
               (castling_rights_black_queen_side ? "q" : "");
        if (fen.EndsWith(' ')) {
            fen += "-";
        }
        fen += " ";
        if (en_passant_target_square == -1) {
            fen += "-";
        } else {
            int x = en_passant_target_square % 8;
            int y = en_passant_target_square / 8;
            fen += $"{(char)('a' + x)}{8 - y}";
        }

        fen += " " + halfmove_clock + " " + fullmove_number;

        // Reverse the board list back to original
        board.Reverse();
        return fen;
    }

    public void FromFEN(string fen) {
        string[] parts = fen.Split(' ');

        // Parse the board
        board = [];
        string[] rows = parts[0].Split('/');
        foreach (string row in rows) {
            string boardRow = "";
            foreach (char c in row) {
                if (char.IsDigit(c)) {
                    int emptyCount = c - '0';
                    boardRow += new string('.', emptyCount);
                } else {
                    boardRow += c;
                }
            }
            board.Add(boardRow);
        }

        // Parse the rest of the FEN
        white_to_move = parts[1] == "w";
        castling_rights_white_king_side = parts[2].Contains('K');
        castling_rights_white_queen_side = parts[2].Contains('Q');
        castling_rights_black_king_side = parts[2].Contains('k');
        castling_rights_black_queen_side = parts[2].Contains('q');
        en_passant_target_square = parts[3] == "-" ? -1 : (parts[3][0] - 'a') + ((8 - (parts[3][1] - '0')) * 8);
        halfmove_clock = int.Parse(parts[4]);
        fullmove_number = int.Parse(parts[5]);

        // Reverse the board to match the original orientation
        board.Reverse();
    }

    public BoardState (string fen) {
        FromFEN(fen);
    }
}
