using Raylib_cs;
using System.Numerics;


class Program {
    static Color light_square_color = new(0xff, 0xe6, 0xcc);
    static Color dark_square_color = new(0x80, 0x42, 0x1c);

    static readonly int board_size = 600;

    static int SquareSize => board_size / 8;

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
        Raylib.InitWindow(board_size, board_size, "Chess");

        BoardState boardState = new BoardState
        {
            board = new List<string>
            {
                "rnbqkbnr",
                "pppppppp",
                "........",
                "........",
                "........",
                "........",
                "PPPPPPPP",
                "RNBQKBNR"
            },
            white_to_move = true,
            castling_rights_white_king_side = true,
            castling_rights_white_queen_side = true,
            castling_rights_black_king_side = true,
            castling_rights_black_queen_side = true,
            en_passant_target_square = -1,
            halfmove_clock = 0,
            fullmove_number = 1
        };

        Dictionary<string, Texture2D> pieceSprites = [];
        foreach (var piece in pieceImages) {
            Texture2D texture = Raylib.LoadTexture(piece.Value);
            pieceSprites.Add(piece.Key, texture);
        }


        int holdIndex = -1;
        bool isDragging = false;

        while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();

            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Raylib.DrawRectangle(i * SquareSize, j * SquareSize, SquareSize, SquareSize, (i + j) % 2 == 0 ? light_square_color : dark_square_color);
                    // Draw the pieces
                    char piece = boardState.board[j][i];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString()) && !(holdIndex == (j * 8 + i) && isDragging)) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(i * SquareSize, j * SquareSize, SquareSize, SquareSize),
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
                int y = (int)(mousePos.Y / SquareSize);

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
                int y = (int)(mousePos.Y / SquareSize);

                if (x >= 0 && x < 8 && y >= 0 && y < 8) {
                    char piece = boardState.board[holdIndex / 8][holdIndex % 8];
                    boardState.board[holdIndex / 8] = boardState.board[holdIndex / 8].Remove(holdIndex % 8, 1).Insert(holdIndex % 8, ".");
                    boardState.board[y] = boardState.board[y].Remove(x, 1).Insert(x, piece.ToString());
                }

                holdIndex = -1;
                isDragging = false;
            }


            if (Raylib.IsKeyPressed(KeyboardKey.F)) {
                string fen = boardState.ToFEN();
                Console.WriteLine(fen);
            }
        }

        Raylib.CloseWindow();
    }
}


class BoardState {
    public required List<string> board;
    public bool white_to_move;
    public bool castling_rights_white_king_side;
    public bool castling_rights_white_queen_side;
    public bool castling_rights_black_king_side;
    public bool castling_rights_black_queen_side;
    public int en_passant_target_square;
    public int halfmove_clock;
    public int fullmove_number;

    public string ToFEN() {
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
        return fen;
    }
}
