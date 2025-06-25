using System;
using Raylib_cs;
using System.Numerics;

using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Texture2D = Raylib_cs.Texture2D;
using Rectangle = Raylib_cs.Rectangle;

namespace ChessInterface {
    public class BoardDrawer {
        static Color light_square_color = new(0xff, 0xe6, 0xcf);
        static Color dark_square_color = new(0x80, 0x42, 0x1c);
        static readonly Dictionary<string, string> pieceImages = new() {
            { "K", "assets/images/wk.png" },
            { "Q", "assets/images/wq.png" },
            { "R", "assets/images/wr.png" },
            { "B", "assets/images/wb.png" },
            { "N", "assets/images/wn.png" },
            { "P", "assets/images/wp.png" },
            { "k", "assets/images/bk.png" },
            { "q", "assets/images/bq.png" },
            { "r", "assets/images/br.png" },
            { "b", "assets/images/bb.png" },
            { "n", "assets/images/bn.png" },
            { "p", "assets/images/bp.png" }
        };

        private Dictionary<string, Texture2D> pieceSprites;

        public BoardDrawer() {
            pieceSprites = [];
            foreach (var piece in pieceImages) {
                Texture2D texture = Raylib.LoadTexture(piece.Value);
                pieceSprites.Add(piece.Key, texture);
            }
        }

        public void DrawBoard(int boardSize, int boardOffsetX, int boardOffsetY) {
            int squareSize = boardSize / 8;

            string lastMove = ChessApp.instance.moveHistory.Count > 0 ? ChessApp.instance.moveHistory.Last() : null;
            int holdIndex = ChessApp.instance.holdIndex;
            bool isDragging = ChessApp.instance.isDragging;

            // Make a move list of moves to render based on the start of the move (the dragging piece)
            List<string> moveRenderList = [];
            foreach (string move in ChessApp.instance.moves) {
                if (move.StartsWith($"{(char)('a' + holdIndex % 8)}{(char)('1' + (holdIndex / 8))}")) {
                    moveRenderList.Add(move);
                }
            }

            for (int file = 0; file < 8; file++) {
                for (int rank = 0; rank < 8; rank++) {
                    int draw_rank = 7 - rank; // Flip the rank for drawing

                    // Draw squares
                    Raylib.DrawRectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize, (file + rank) % 2 == 0 ? light_square_color : dark_square_color);

                    foreach (string move in moveRenderList) {
                        if (move[2] == (char)('a' + file) && move[3] == (char)('1' + rank)) {
                            Raylib.DrawRectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize, new Color(255, 0, 0, 255 / 2)); // Highlight valid moves in red
                            break;
                        }
                    }

                    if (holdIndex == (rank * 8 + file) && isDragging) {
                        Raylib.DrawRectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize, new Color(255, 255, 0, 255 / 2)); // Highlight the square being dragged in yellow
                    }

                    if (lastMove != null && lastMove[0] == (char)('a' + file) && lastMove[1] == (char)('1' + rank)) {
                        Raylib.DrawRectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize, new Color(100, 100, 255, 255 / 2)); // Highlight the last move's start square in green
                    } else if (lastMove != null && lastMove[2] == (char)('a' + file) && lastMove[3] == (char)('1' + rank)) {
                        Raylib.DrawRectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize, new Color(100, 100, 255, 255 / 2)); // Highlight the last move's end square in blue
                    }

                    // Draw the pieces
                    char piece = ChessApp.instance.boardState.board[rank][file];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString()) && !(holdIndex == (rank * 8 + file) && isDragging)) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(boardOffsetX + file * squareSize, boardOffsetY + draw_rank * squareSize, squareSize, squareSize),
                            Vector2.Zero,
                            0,
                            Color.White
                        );
                    }
                }
            }

            // Draw the dragged piece if applicable
            if (isDragging && holdIndex != -1) {
                Vector2 mousePos = Raylib.GetMousePosition();

                if (holdIndex >= 0) {
                    int x = holdIndex % 8;
                    int yBoard = holdIndex / 8;

                    char piece = ChessApp.instance.boardState.board[yBoard][x];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString())) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(mousePos.X - squareSize / 2, (mousePos.Y - boardOffsetY) - squareSize / 2, squareSize, squareSize),
                            Vector2.Zero,
                            0,
                            Color.White
                        );
                    }
                }
            }
        }

        public void HandleBoardInteraction(int boardSize, int boardOffsetX, int boardOffsetY) {
            int squareSize = boardSize / 8;

            do {
                if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                    Vector2 mousePos = Raylib.GetMousePosition();
                    if ((mousePos.X - boardOffsetX) < 0 || (mousePos.X - boardOffsetX) > boardSize || (mousePos.Y - boardOffsetY) < 0 || (mousePos.Y - boardOffsetY) > boardSize) {
                        // If mouse is outside the board, reset holdIndex and isDragging
                        ChessApp.instance.holdIndex = -1;
                        ChessApp.instance.isDragging = false;
                        break;
                    }
                    int x = (int)((mousePos.X - boardOffsetX) / squareSize);
                    int y = 7 - (int)((mousePos.Y - boardOffsetY) / squareSize);

                    if (x >= 0 && x < 8 && y >= 0 && y < 8) {
                        char piece = ChessApp.instance.boardState.board[y][x];
                        if (piece != '.') {
                            ChessApp.instance.holdIndex = y * 8 + x;
                            ChessApp.instance.isDragging = true;
                        }
                    }
                }
            } while (false);

            do {
                if (Raylib.IsMouseButtonReleased(MouseButton.Left) && ChessApp.instance.isDragging) {
                    Vector2 mousePos = Raylib.GetMousePosition();

                    // check if mouse on the board
                    if ((mousePos.X - boardOffsetX) < 0 || (mousePos.X - boardOffsetX) > boardSize || (mousePos.Y - boardOffsetY) < 0 || (mousePos.Y - boardOffsetY) > boardSize) {
                        ChessApp.instance.holdIndex = -1;
                        ChessApp.instance.isDragging = false;
                        break;
                    }

                    int x = (int)((mousePos.X - boardOffsetX) / squareSize);
                    int y = 7 - (int)((mousePos.Y - boardOffsetY) / squareSize);

                    // if they are the same square, just ignore
                    if (ChessApp.instance.holdIndex == y * 8 + x) {
                        ChessApp.instance.holdIndex = -1;
                        ChessApp.instance.isDragging = false;
                        break;
                    }

                    if (x >= 0 && x < 8 && y >= 0 && y < 8) {

                        string generatedMove = $"{(char)('a' + ChessApp.instance.holdIndex % 8)}{(char)('1' + (ChessApp.instance.holdIndex / 8))}{(char)('a' + x)}{(char)('1' + y)}";

                        // Add move from move list if that's in there
                        if (ChessApp.instance.moves.Count > 0 && ChessApp.instance.moves.Any(m => m.StartsWith(generatedMove))) {
                            char piece = ChessApp.instance.boardState.board[ChessApp.instance.holdIndex / 8][ChessApp.instance.holdIndex % 8];
                            ChessApp.instance.boardState.board[ChessApp.instance.holdIndex / 8] = ChessApp.instance.boardState.board[ChessApp.instance.holdIndex / 8].Remove(ChessApp.instance.holdIndex % 8, 1).Insert(ChessApp.instance.holdIndex % 8, ".");
                            ChessApp.instance.boardState.board[y] = ChessApp.instance.boardState.board[y].Remove(x, 1).Insert(x, piece.ToString());

                            // check if amount of moves is more than 1, if so, that means it's a promotion and it needs to pick the one based on preference
                            if (ChessApp.instance.moves.Count(m => m.StartsWith(generatedMove)) > 1) {
                                ChessApp.instance.moveHistory.Add(generatedMove + ChessApp.instance.promotionPiece);
                            } else {
                                // find the move that matches the holdIndex
                                string move = ChessApp.instance.moves.First(m => m.StartsWith(generatedMove));
                                ChessApp.instance.moveHistory.Add(move);
                            }

                            // clear moves
                            ChessApp.instance.moves.Clear();

                            // Run bot move in a background thread if needed
                            if (
                                (ChessApp.instance.boardState.white_to_move && ChessApp.instance.botHandler.selectedBotWhite == 0 && ChessApp.instance.botHandler.selectedBotBlack != 0) ||
                                (!ChessApp.instance.boardState.white_to_move && ChessApp.instance.botHandler.selectedBotBlack == 0 && ChessApp.instance.botHandler.selectedBotWhite != 0)
                            ) {
                                string move = ChessApp.instance.boardState.white_to_move
                                    ? ChessApp.instance.botHandler.askMoveTryAgain(ChessApp.instance.botHandler.botExecutablePathB, false)
                                    : ChessApp.instance.botHandler.askMoveTryAgain(ChessApp.instance.botHandler.botExecutablePathA, true);
                                if (move != null) {
                                    ChessApp.instance.UpdateBoardState();
                                    ChessApp.instance.moveHistory.Add(move);
                                    ChessApp.instance.UpdateBoardState();
                                }
                            } else {
                                ChessApp.instance.UpdateBoardState();
                            }
                        }
                    }

                    ChessApp.instance.promotionPiece = 'q'; // Reset promotion piece to queen after a move
                    ChessApp.instance.holdIndex = -1;
                    ChessApp.instance.isDragging = false;
                }
            } while (false);
        }
    }
}
