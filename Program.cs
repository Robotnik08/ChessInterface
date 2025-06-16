using Raylib_cs;
using System.Numerics;
using System.Diagnostics;
using System.Windows.Forms;

using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Texture2D = Raylib_cs.Texture2D;
using Rectangle = Raylib_cs.Rectangle;

enum State {
    None,
    CheckMate,
    StaleMate,
    ThreefoldRepetition,
    FiftyMoveRule,
    InsufficientMaterial,
}

class Program {
    static Color light_square_color = new(0xff, 0xe6, 0xcc);
    static Color dark_square_color = new(0x80, 0x42, 0x1c);

    static readonly int board_size = 800;

    static int SquareSize => board_size / 8;

    static readonly string logFolder = "C:/Users/Sebastiaan Heins/Downloads/chess-c/logs/";

    static readonly string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    static readonly string enginePath = "C:/Users/Sebastiaan Heins/Downloads/chess-c/build/chess.exe";

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

    [STAThread]
    static void Main() {
        // UI State
        BoardState boardState = new(startFEN);
        State state = State.None;

        int holdIndex = -1;
        bool isDragging = false;

        char promotionPiece = 'q'; // Default promotion piece, can be changed later

        List<string> moves = [];
        List<string> moveHistory = [];

        UpdateBoardState();

        // UI variables
        int uiWidth = 350;
        int windowWidth = board_size + uiWidth;
        int windowHeight = board_size;

        Raylib.InitWindow(windowWidth, windowHeight, "Chess");

        Dictionary<string, Texture2D> pieceSprites = [];
        foreach (var piece in pieceImages) {
            Texture2D texture = Raylib.LoadTexture(piece.Value);
            pieceSprites.Add(piece.Key, texture);
        }

        Image iconTexture = Raylib.LoadImage("assets/bp.png");
        Raylib.SetWindowIcon(iconTexture);
        Raylib.SetTargetFPS(60);

        // Bot selection UI state
        string[] botOptions = ["Human", "Select"];
        string botExecutablePathA = null;
        bool botASide = true;
        string botExecutablePathB = null;
        int selectedBotWhite = 0;
        int selectedBotBlack = 0;
        int numGames = 10;
        string currentLogFile = null;
        bool runningGames = false;
        int wins = 0, draws = 0, losses = 0, gamesPlayed = 0;

        Rectangle boardRect = new Rectangle(uiWidth, 0, board_size, board_size);

        while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            // --- UI Panel ---
            int panelX = 0;
            int panelY = 0;
            int panelW = uiWidth;
            int panelH = windowHeight;
            int pos_y = 20;
            int spacing = 40;

            Raylib.DrawRectangle(panelX, panelY, panelW, panelH, new Color(240, 240, 240, 255));
            Raylib.DrawText("Bot Selection", panelX + 20, pos_y, 24, Color.Black);
            pos_y += spacing;

            // A bot dropdown
            Raylib.DrawText("A:", panelX + 20, pos_y, 20, Color.Black);
            for (int i = 0; i < botOptions.Length; i++) {
                Rectangle btn = new(panelX + 90, pos_y + i * 28, 80, 24);
                Raylib.DrawRectangleRec(btn, i == selectedBotWhite ? Color.LightGray : Color.Gray);
                Raylib.DrawText(botOptions[i], (int)btn.X + 8, (int)btn.Y + 4, 18, Color.Black);
                if (i == 1) {
                    Raylib.DrawText((botExecutablePathA ?? "No bot selected").Split("\\").Last(), (int)btn.X + 8 + 80, (int)btn.Y + 4, 18, Color.Red);
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btn) && !runningGames) {
                    if (i == 1) {
                        // Open file dialog to select bot executable
                        OpenFileDialog openFileDialog = new() {
                            Filter = "Executable files (*.exe)|*.exe",
                            Title = "Select Bot Executable"
                        };
                        if (openFileDialog.ShowDialog() == DialogResult.OK) {
                            botExecutablePathA = openFileDialog.FileName;
                            selectedBotWhite = i;
                            if (selectedBotBlack == 0) {
                                // make the first move against the human
                                boardState.FromFEN(startFEN);
                                moveHistory.Clear();
                                botASide = true; // A bot is white
                                string move = askMove(botExecutablePathA);
                                moveHistory.Add(move);
                                UpdateBoardState();
                            }
                        } else {
                            botExecutablePathA = null;
                        }
                    } else {
                        selectedBotWhite = i;
                        botExecutablePathA = null;
                    }
                }
            }
            pos_y += botOptions.Length * 28 + 10;

            // B bot dropdown
            Raylib.DrawText("B:", panelX + 20, pos_y, 20, Color.Black);
            for (int i = 0; i < botOptions.Length; i++) {
                Rectangle btn = new(panelX + 90, pos_y + i * 28, 80, 24);
                Raylib.DrawRectangleRec(btn, i == selectedBotBlack ? Color.LightGray : Color.Gray);
                Raylib.DrawText(botOptions[i], (int)btn.X + 8, (int)btn.Y + 4, 18, Color.Black);
                if (i == 1) {
                    Raylib.DrawText((botExecutablePathB ?? "No bot selected").Split("\\").Last(), (int)btn.X + 8 + 80, (int)btn.Y + 4, 18, Color.Red);
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btn) && !runningGames) {
                    if (i == 1) {
                        // Open file dialog to select bot executable
                        OpenFileDialog openFileDialog = new() {
                            Filter = "Executable files (*.exe)|*.exe",
                            Title = "Select Bot Executable"
                        };
                        if (openFileDialog.ShowDialog() == DialogResult.OK) {
                            selectedBotBlack = i;
                            botExecutablePathB = openFileDialog.FileName;
                        } else {
                            botExecutablePathB = null;
                        }
                    } else {
                        selectedBotBlack = i;
                        botExecutablePathB = null;
                    }
                }
            }
            pos_y += botOptions.Length * 28 + 10;

            // Number of games input
            Raylib.DrawText("Games to test:", panelX + 20, pos_y, 20, Color.Black);
            Rectangle numGamesBox = new(panelX + 160, pos_y, 60, 28);
            Raylib.DrawRectangleRec(numGamesBox, Color.White);
            Raylib.DrawRectangleLines((int)numGamesBox.X, (int)numGamesBox.Y, (int)numGamesBox.Width, (int)numGamesBox.Height, Color.Black);
            Raylib.DrawText(numGames.ToString(), (int)numGamesBox.X + 8, (int)numGamesBox.Y + 4, 20, Color.Black);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), numGamesBox)) {
                numGames = (numGames % 1000) + 10;
            } else if (Raylib.IsMouseButtonPressed(MouseButton.Right) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), numGamesBox)) {
                numGames = (numGames - 20 + 1000) % 1000 + 10; // Wrap around to keep it positive
            }
            pos_y += spacing;

            // Results bar
            Raylib.DrawText("Results:", panelX + 20, pos_y, 20, Color.Black);
            pos_y += 28;
            int barW = panelW - 40;
            int barH = 28;
            int total = wins + draws + losses;
            float winFrac = total > 0 ? (float)wins / total : 0;
            float drawFrac = total > 0 ? (float)draws / total : 0;
            float lossFrac = total > 0 ? (float)losses / total : 0;
            int winW = (int)(barW * winFrac);
            int drawW = (int)(barW * drawFrac);
            int lossW = barW - winW - drawW;
            int barX = panelX + 20;
            int barY = pos_y;
            Raylib.DrawRectangle(barX, barY, winW, barH, Color.Green);
            Raylib.DrawRectangle(barX + winW, barY, drawW, barH, Color.Yellow);
            Raylib.DrawRectangle(barX + winW + drawW, barY, lossW, barH, Color.Red);
            Raylib.DrawRectangleLines(barX, barY, barW, barH, Color.Black);
            Raylib.DrawText($"W: {wins}  D: {draws}  L: {losses}", barX + 5, barY + 4, 18, Color.Black);
            pos_y += barH + 20;

            // Start/Stop buttons
            Rectangle startBtn = new(panelX + 20, pos_y, 120, 36);
            Rectangle stopBtn = new(panelX + 160, pos_y, 120, 36);
            Raylib.DrawRectangleRec(startBtn, runningGames ? Color.Gray : Color.SkyBlue);
            Raylib.DrawText("Start", (int)startBtn.X + 30, (int)startBtn.Y + 8, 22, Color.Black);
            Raylib.DrawRectangleRec(stopBtn, runningGames ? Color.SkyBlue : Color.Gray);
            Raylib.DrawText("Stop", (int)stopBtn.X + 35, (int)stopBtn.Y + 8, 22, Color.Black);

            if (!runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), startBtn)) {
                if (selectedBotWhite == 0 || selectedBotBlack == 0) {
                    MessageBox.Show("Please select both bots before starting the games.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                runningGames = true;
                wins = draws = losses = 0;

                // append to log file	
                currentLogFile = $"{logFolder}chess_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                // Create the log file if it doesn't exist
                if (!System.IO.Directory.Exists(logFolder)) {
                    System.IO.Directory.CreateDirectory(logFolder);
                }

                using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                    logWriter.WriteLine($"==============================================");
                    logWriter.WriteLine($"\tStarting games at {DateTime.Now}");
                    logWriter.WriteLine($"\tBot A: {botExecutablePathA ?? "Human"}");
                    logWriter.WriteLine($"\tBot B: {botExecutablePathB ?? "Human"}");
                    logWriter.WriteLine($"\tNumber of games: {numGames}");
                    logWriter.WriteLine($"==============================================");
                    logWriter.WriteLine();


                    logWriter.WriteLine($"=======================");
                    logWriter.WriteLine($"  Game {1}");
                    logWriter.WriteLine($"  Starting FEN: {startFEN}");
                    logWriter.WriteLine($"=======================");
                    logWriter.WriteLine();
                }
            }


            if (runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), stopBtn)) {
                runningGames = false;
            }

            // --- Chess Board ---
            // Offset all board drawing by uiWidth
            int boardOffsetX = uiWidth;

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

                    // Draw squares
                    Raylib.DrawRectangle(boardOffsetX + file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, (file + rank) % 2 == 0 ? light_square_color : dark_square_color);

                    foreach (string move in moveRenderList) {
                        if (move[2] == (char)('a' + file) && move[3] == (char)('1' + rank)) {
                            Raylib.DrawRectangle(boardOffsetX + file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, new Color(255, 0, 0, 255 / 2)); // Highlight valid moves in red
                            break;
                        }
                    }
                    if (holdIndex == (rank * 8 + file) && isDragging) {
                        Raylib.DrawRectangle(boardOffsetX + file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize, new Color(255, 255, 0, 255 / 2)); // Highlight the square being dragged in yellow
                    }

                    // Draw the pieces
                    char piece = boardState.board[rank][file];
                    if (piece != '.' && pieceSprites.ContainsKey(piece.ToString()) && !(holdIndex == (rank * 8 + file) && isDragging)) {
                        Texture2D texture = pieceSprites[piece.ToString()];
                        Raylib.DrawTexturePro(
                            texture,
                            new Rectangle(0, 0, texture.Width, texture.Height),
                            new Rectangle(boardOffsetX + file * SquareSize, draw_rank * SquareSize, SquareSize, SquareSize),
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
                    int yBoard = holdIndex / 8;

                    char piece = boardState.board[yBoard][x];
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
                if ((mousePos.X - boardOffsetX) < 0 || (mousePos.X - boardOffsetX) > board_size || mousePos.Y < 0 || mousePos.Y > board_size) {
                    // If mouse is outside the board, reset holdIndex and isDragging
                    holdIndex = -1;
                    isDragging = false;
                    continue;
                }
                int x = (int)((mousePos.X - boardOffsetX) / SquareSize);
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
                if ((mousePos.X - boardOffsetX) < 0 || (mousePos.X - boardOffsetX) > board_size || mousePos.Y < 0 || mousePos.Y > board_size) {
                    holdIndex = -1;
                    isDragging = false;
                    continue;
                }

                int x = (int)((mousePos.X - boardOffsetX) / SquareSize);
                int y = 7 - (int)(mousePos.Y / SquareSize);

                // if they are the same square, just ignore
                if (holdIndex == y * 8 + x) {
                    holdIndex = -1;
                    isDragging = false;
                    continue;
                }

                if (x >= 0 && x < 8 && y >= 0 && y < 8) {

                    string generatedMove = $"{(char)('a' + holdIndex % 8)}{(char)('1' + (holdIndex / 8))}{(char)('a' + x)}{(char)('1' + y)}";
                    // Add move from move list if that's in there
                    if (moves.Count > 0 && moves.Any(m => m.StartsWith(generatedMove))) {
                        char piece = boardState.board[holdIndex / 8][holdIndex % 8];
                        boardState.board[holdIndex / 8] = boardState.board[holdIndex / 8].Remove(holdIndex % 8, 1).Insert(holdIndex % 8, ".");
                        boardState.board[y] = boardState.board[y].Remove(x, 1).Insert(x, piece.ToString());

                        // check if amount of moves is more than 1, if so, that means it's a promotion and it needs to pick the one based on preference
                        if (moves.Count(m => m.StartsWith(generatedMove)) > 1) {
                            moveHistory.Add(generatedMove + promotionPiece);
                        } else {
                            // find the move that matches the holdIndex
                            string move = moves.First(m => m.StartsWith(generatedMove));
                            moveHistory.Add(move);
                        }

                        if (boardState.white_to_move && selectedBotWhite == 0) {
                            // The player made a move, ask for the bot's move
                            string move = askMove(botExecutablePathB);
                            moveHistory.Add(move);
                        } else if (!boardState.white_to_move && selectedBotBlack == 0) {
                            // The player made a move, ask for the bot's move
                            string move = askMove(botExecutablePathA);
                            moveHistory.Add(move);
                        }

                        UpdateBoardState();
                    }
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

            if (runningGames) {
                // If the engine is running, ask it for a move
                string move = askMove(boardState.white_to_move == botASide ? botExecutablePathA : botExecutablePathB);
                if (move != null) {
                    // Add the move to the history
                    moveHistory.Add(move);
                    UpdateBoardState();

                    if (state == State.CheckMate) {
                        wins += boardState.white_to_move && botASide == boardState.white_to_move ? 1 : 0;
                        losses += boardState.white_to_move && botASide == boardState.white_to_move ? 0 : 1;
                    } else if (state != State.None) {
                        draws++;
                    }

                    if (state != State.None && gamesPlayed < numGames) {
                        if (++gamesPlayed >= numGames) {
                            runningGames = false;
                            // save to log file
                        }
                        using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                            logWriter.WriteLine($"=======================");
                            logWriter.WriteLine($"  Result Game {gamesPlayed}");
                            logWriter.WriteLine($"  Result: {(state == State.CheckMate ? (boardState.white_to_move ? "Black wins" : "White wins") : "Draw")}");
                            logWriter.WriteLine($"  Ending FEN: {boardState.ToFEN()}");
                            logWriter.WriteLine($"=======================");
                            logWriter.WriteLine();

                            if (runningGames) {
                                logWriter.WriteLine($"=======================");
                                logWriter.WriteLine($"  Game {gamesPlayed + 1}");
                                logWriter.WriteLine($"  Starting FEN: {startFEN}");
                                logWriter.WriteLine($"=======================");
                                logWriter.WriteLine();
                            } else {
                                logWriter.WriteLine($"==============================================");
                                logWriter.WriteLine($"\tFinished all games at {DateTime.Now}");
                                logWriter.WriteLine($"\tTotal games played: {gamesPlayed}");
                                logWriter.WriteLine($"\tWins: {wins}, Draws: {draws}, Losses: {losses}");
                                logWriter.WriteLine($"==============================================");
                            }
                        }

                        boardState.FromFEN(startFEN);
                        moveHistory.Clear();
                        botASide = !botASide;
                    }
                }
            }
        }

        Raylib.CloseWindow();

        void UpdateBoardState() {
            // Run engine/chess.exe and ask for legal moves and draw them
            Process engineProcess = new();
            engineProcess.StartInfo.FileName = enginePath;
            engineProcess.StartInfo.Arguments = "engine";
            engineProcess.StartInfo.UseShellExecute = false;
            engineProcess.StartInfo.RedirectStandardInput = true;
            engineProcess.StartInfo.RedirectStandardOutput = true;
            engineProcess.StartInfo.CreateNoWindow = true;
            engineProcess.Start();

            StreamWriter engineInput = engineProcess.StandardInput;
            StreamReader engineOutput = engineProcess.StandardOutput;

            engineInput.WriteLine("setfen");
            engineInput.WriteLine(startFEN);
            // check if response is ok
            string response = engineOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting FEN: {response}");
                return;
            }

            // Sending move history to the engine
            string movesString = string.Join(" ", moveHistory);
            engineInput.WriteLine("setmovehistory");
            engineInput.WriteLine(movesString);
            // check if response is ok
            response = engineOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting move history: {response}");
                return;
            }

            engineInput.WriteLine("getmoves");

            moves = [];
            while (true) {
                string line = engineOutput.ReadLine();
                if (line == null || line == "ok") {
                    break;
                }
                moves.Add(line);
            }

            // Get FEN from the engine
            engineInput.WriteLine("getfen");
            string fen = engineOutput.ReadLine();
            // check if response is ok
            if (fen == null || fen == "ok") {
                Console.WriteLine($"Error getting FEN: {fen}");
                return;
            }
            if (engineOutput.ReadLine() != "ok") {
                Console.WriteLine("Error getting FEN: expected 'ok' response");
                return;
            }
            boardState.FromFEN(fen);


            // Ask for board state
            engineInput.WriteLine("getstate");
            string boardStateResponse = engineOutput.ReadLine();
            if (boardStateResponse == null || boardStateResponse == "ok") {
                Console.WriteLine($"Error getting board state: {boardStateResponse}");
                return;
            }
            if (engineOutput.ReadLine() != "ok") {
                Console.WriteLine("Error getting board state: expected 'ok' response");
                return;
            }
            // Parse the board state
            switch (boardStateResponse) {
                case "checkmate":
                    state = State.CheckMate;
                    break;
                case "stalemate":
                    state = State.StaleMate;
                    break;
                case "threefold_repetition":
                    state = State.ThreefoldRepetition;
                    break;
                case "fifty_move_draw":
                    state = State.FiftyMoveRule;
                    break;
                case "insufficient_material":
                    state = State.InsufficientMaterial;
                    break;
                default:
                    state = State.None;
                    break;
            }


            engineInput.WriteLine("end");
            engineProcess.WaitForExit();
            if (engineProcess.ExitCode != 0) {
                Console.WriteLine($"Engine process exited with code {engineProcess.ExitCode}");
                return;
            }
        }

        string askMove(string botExecutablePath) {
            if (string.IsNullOrEmpty(botExecutablePath)) {
                Console.WriteLine("No bot executable path provided.");
                return null;
            }

            // log file
            if (!System.IO.Directory.Exists(logFolder)) {
                System.IO.Directory.CreateDirectory(logFolder);
            }
            if (currentLogFile == null) {
                currentLogFile = $"{logFolder}chess_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            }
            using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                logWriter.WriteLine($"Asking bot for move at {DateTime.Now}");
                logWriter.WriteLine($"FEN: {boardState.ToFEN()}");
                logWriter.WriteLine($"Move history: {string.Join(" ", moveHistory)}");
                logWriter.WriteLine();
            }

            Process botProcess = new();
            botProcess.StartInfo.FileName = botExecutablePath;
            botProcess.StartInfo.Arguments = "engine";
            botProcess.StartInfo.UseShellExecute = false;
            botProcess.StartInfo.RedirectStandardInput = true;
            botProcess.StartInfo.RedirectStandardOutput = true;
            botProcess.StartInfo.CreateNoWindow = true;

            botProcess.Start();

            StreamWriter botInput = botProcess.StandardInput;
            StreamReader botOutput = botProcess.StandardOutput;

            botInput.WriteLine("setfen");
            botInput.WriteLine(startFEN);
            // check if response is ok
            string response = botOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting FEN: {response}");
                return null;
            }

            // Sending move history to the engine
            string movesString = string.Join(" ", moveHistory);
            botInput.WriteLine("setmovehistory");
            botInput.WriteLine(movesString);
            // check if response is ok
            response = botOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine($"Error setting move history: {response}");
                return null;
            }


            // Ask for move to play
            botInput.WriteLine("getmove");
            botInput.WriteLine("1"); // 1 second of thinking time
            string move = botOutput.ReadLine();
            if (move == null || move == "ok") {
                Console.WriteLine($"Error getting move: {move}");
                return null;
            }
            response = botOutput.ReadLine();
            if (response != "ok") {
                Console.WriteLine("Error getting move: expected 'ok' response, got: " + response);
                return null;
            }


            botInput.WriteLine("end");
            botProcess.WaitForExit();
            if (botProcess.ExitCode != 0) {
                Console.WriteLine($"Bot process exited with code {botProcess.ExitCode}");
                return null;
            }

            return move;
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
