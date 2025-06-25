using Raylib_cs;
using System.Numerics;
using System.Diagnostics;
using System.Windows.Forms;
using ChessInterface;

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

    static Dictionary<string, BotInstance> botInstances = new();

    static readonly int board_size = 800;

    static int SquareSize => board_size / 8;

    static readonly string logFolder = "logs/";

    static readonly string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    static readonly string startMoves = "";

    static readonly int thinkingTimeMs = 100; // Time in milliseconds for the bot to think

    static readonly string enginePath = "C:/Users/Sebastiaan Heins/Downloads/chess-c/build/chess.exe";

    static readonly string fensFile = "assets/fens/fens.txt";

    [STAThread]
    static void Main() {
        // UI State
        BoardState boardState = new(startFEN);
        State state = State.None;

        bool thinking = false;

        List<string> fens = [.. File.ReadAllLines(fensFile)];
        int fen_index = 0;

        int holdIndex = -1;
        bool isDragging = false;

        char promotionPiece = 'q'; // Default promotion piece, can be changed later

        List<string> moves = [];
        List<string> moveHistory = string.IsNullOrWhiteSpace(startMoves) ? [] : startMoves.Split(' ').ToList();

        // UI variables
        int uiWidth = 350;
        int windowWidth = board_size + uiWidth;
        int windowHeight = board_size;

        Raylib.InitWindow(windowWidth, windowHeight, "Chess interface " + "version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        BoardDrawer boardDrawer = new();

        Image iconTexture = Raylib.LoadImage("assets/images/bp.png");
        Raylib.SetWindowIcon(iconTexture);
        Raylib.SetTargetFPS(60);

        // Bot selection UI state
        string[] botOptions = ["Human", "Select"];
        string botExecutablePathA = null;
        bool botASide = true;
        string botExecutablePathB = null;
        int selectedBotWhite = 0;
        int selectedBotBlack = 0;
        int numGames = 500;
        string currentLogFile = null;
        bool runningGames = false;
        int wins = 0, draws = 0, losses = 0, gamesPlayed = 0;

        int botALastDepth = 0;
        int botBLastDepth = 0;
        int botALastEval = 0;
        int botBLastEval = 0;

        Rectangle boardRect = new(uiWidth, 0, board_size, board_size);

        UpdateBoardState();

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
                                string move = askMoveTryAgain(botExecutablePathA, true);

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

            // when games are running, say which side is which bot
            if (runningGames) {
                Raylib.DrawText($"Bot A: {(botASide ? "White" : "Black")}", panelX + 20, pos_y + 50, 20, Color.Black);
                Raylib.DrawText($"Bot B: {(!botASide ? "White" : "Black")}", panelX + 20, pos_y + 80, 20, Color.Black);

                // also draw the last depth and eval of the bots
                Raylib.DrawText($"Bot A Depth: {convertDepth(botALastDepth)}, Eval: {convertEval(botALastEval)}", panelX + 20, pos_y + 110, 20, Color.Black);
                Raylib.DrawText($"Bot B Depth: {convertDepth(botBLastDepth)}, Eval: {convertEval(botBLastEval)}", panelX + 20, pos_y + 140, 20, Color.Black);
            }

            if (!runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), startBtn)) {
                if (selectedBotWhite == 0 || selectedBotBlack == 0) {
                    MessageBox.Show("Please select both bots before starting the games.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                runningGames = true;
                wins = draws = losses = 0;
                gamesPlayed = 0;
                fen_index = 0;
                ClearBotEngines();

                boardState.FromFEN(fens[fen_index % fens.Count]);
                moveHistory.Clear();
                botASide = true; // Start with Bot A as white

                UpdateBoardState(fens[fen_index % fens.Count]);

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
                    logWriter.WriteLine($"  Game {gamesPlayed + 1}");
                    logWriter.WriteLine($"  Starting FEN: {fens[fen_index % fens.Count]}");
                    logWriter.WriteLine($"  White: {(botASide ? "Bot A" : "Bot B")}");
                    logWriter.WriteLine($"  Black: {(botASide ? "Bot B" : "Bot A")}");
                    logWriter.WriteLine($"=======================");
                    logWriter.WriteLine();
                }
            }


            if (runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), stopBtn)) {
                runningGames = false;
            }

            boardDrawer.DrawBoard(800, uiWidth, 0, boardState, moves, holdIndex, isDragging, lastMove: moveHistory.Count > 0 ? moveHistory.Last() : null);


            Raylib.EndDrawing();


            if (Raylib.IsKeyPressed(KeyboardKey.F)) {
                string fen = boardState.ToFEN();
                Console.WriteLine(fen);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.C) && !runningGames) {
                // Clear the board to the starting position
                boardState.FromFEN(startFEN);
                moveHistory.Clear();
                UpdateBoardState();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.U) && !runningGames) {
                // Undo the last move
                if (moveHistory.Count > 0) {
                    moveHistory.RemoveAt(moveHistory.Count - 1);
                    UpdateBoardState();
                    // bots are too stupid to handle this, so we clear them
                    ClearBotEngines();
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.M) && !runningGames) {
                // force the bot to make a move
                if (!runningGames) {
                    string move = askMoveTryAgain(boardState.white_to_move == botASide ? botExecutablePathA : botExecutablePathB, boardState.white_to_move == botASide);
                    if (move != null) {
                        moveHistory.Add(move);
                        UpdateBoardState();
                    }
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Q) && !runningGames) {
                promotionPiece = 'q'; // Queen
            }
            if (Raylib.IsKeyPressed(KeyboardKey.R) && !runningGames) {
                promotionPiece = 'r'; // Rook
            }
            if (Raylib.IsKeyPressed(KeyboardKey.B) && !runningGames) {
                promotionPiece = 'b'; // Bishop
            }
            if (Raylib.IsKeyPressed(KeyboardKey.N) && !runningGames) {
                promotionPiece = 'n'; // Knight
            }

            if (runningGames) {
                // If the engine is running, ask it for a move
                if (!thinking) {
                    thinking = true;
                    string move = askMoveTryAgain(boardState.white_to_move == botASide ? botExecutablePathA : botExecutablePathB, boardState.white_to_move == botASide, fens[fen_index % fens.Count]);
                    if (move != null) {
                        // Add the move to the history
                        moveHistory.Add(move);
                        UpdateBoardState(fens[fen_index % fens.Count]);

                        if (state == State.CheckMate) {
                            losses += botASide == boardState.white_to_move ? 1 : 0;
                            wins += botASide == boardState.white_to_move ? 0 : 1;
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
                                logWriter.WriteLine($"  Result: {(state == State.CheckMate ? (
                                    (botASide == boardState.white_to_move ? "Bot B wins" : "Bot A wins")
                                    + (boardState.white_to_move ? " as black" : " as white")
                                ) : "Draw")}");
                                logWriter.WriteLine($"  Ending FEN: {boardState.ToFEN()}");
                                logWriter.WriteLine($"=======================");
                                logWriter.WriteLine();

                                if (runningGames) {
                                    logWriter.WriteLine($"=======================");
                                    logWriter.WriteLine($"  Game {gamesPlayed + 1}");
                                    logWriter.WriteLine($"  Starting FEN: {fens[++fen_index % fens.Count]}");
                                    logWriter.WriteLine($"  White: {(botASide ? "Bot B" : "Bot A")}");
                                    logWriter.WriteLine($"  Black: {(botASide ? "Bot A" : "Bot B")}");
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

                            boardState.FromFEN(fens[fen_index % fens.Count]);
                            moveHistory.Clear();
                            ClearBotEngines();
                            botASide = !botASide;
                            botALastDepth = 0;
                            botBLastDepth = 0;
                            botALastEval = 0;
                            botBLastEval = 0;
                        }
                    }
                    thinking = false;
                }
            }
        }

        Raylib.CloseWindow();

        void UpdateBoardState(string sfen = null) {
            sfen ??= startFEN;
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
            engineInput.WriteLine(sfen);
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

            if (selectedBotBlack == 0 || selectedBotWhite == 0) {
                // If there is a human player, we announce the current state if it is not None
                if (state != State.None) {
                    string message = state switch {
                        State.CheckMate => "Checkmate! Game over.",
                        State.StaleMate => "Stalemate! Game over.",
                        State.ThreefoldRepetition => "Threefold repetition! Game over.",
                        State.FiftyMoveRule => "Fifty-move rule! Game over.",
                        State.InsufficientMaterial => "Insufficient material! Game over.",
                        _ => ""
                    };
                    if (!string.IsNullOrEmpty(message)) {
                        MessageBox.Show(message, "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        string askMove(string botExecutablePath, bool is_a, string sfen = null) {
            sfen ??= startFEN;

            if (string.IsNullOrEmpty(botExecutablePath)) {
                Console.WriteLine("No bot executable path provided.");
                return null;
            }

            var bot = GetOrStartBot(botExecutablePath);
            var input = bot.Input;
            var output = bot.Output;

            void Send(string line) {
                input.WriteLine(line);
                input.Flush();
            }

            string Receive() {
                string line = output.ReadLine();
                if (line == null) throw new Exception($"Bot {botExecutablePath} closed unexpectedly.");
                return line;
            }

            // Logging
            if (!System.IO.Directory.Exists(logFolder))
                System.IO.Directory.CreateDirectory(logFolder);

            if (currentLogFile == null)
                currentLogFile = $"{logFolder}chess_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                logWriter.WriteLine($"Asking bot for move at {DateTime.Now}");
                logWriter.WriteLine($"FEN: {boardState.ToFEN()}");
                logWriter.WriteLine($"Move history: {string.Join(" ", moveHistory)}");
            }

            // Set FEN
            Send("setfen");
            Send(sfen);
            if (Receive() != "ok") return null;

            // Move history
            Send("setmovehistory");
            Send(string.Join(" ", moveHistory));
            if (Receive() != "ok") return null;

            // Ask for move
            bool move_parsed = false;
            Send("getmove");
            Send(thinkingTimeMs.ToString());
            string move = Receive();
            string next_response = Receive();
            if (next_response != "ok") {
                if (is_a) {
                    botALastDepth = int.Parse(next_response);
                } else {
                    botBLastDepth = int.Parse(next_response);
                }
            } else {
                move_parsed = true;
            }

            next_response = Receive();
            if (next_response != "ok") {
                if (is_a) {
                    botALastEval = int.Parse(next_response);
                } else {
                    botBLastEval = int.Parse(next_response);
                }
            } else {
                move_parsed = true;
            }

            if (!move_parsed && Receive() != "ok") return null;

            int bot_depth = is_a ? botALastDepth : botBLastDepth;
            int bot_eval = is_a ? botALastEval : botBLastEval;
            using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                logWriter.WriteLine($"Bot {botExecutablePath} suggested move: {move}");
                logWriter.WriteLine($"Depth searched: {bot_depth}");
                logWriter.WriteLine($"Evaluation score: {bot_eval}");
                logWriter.WriteLine();
            }

            Console.WriteLine($"Bot {botExecutablePath} Depth: {bot_depth}, Eval {bot_eval}, Move: {move}");

            return move;
        }

        string askMoveTryAgain(string botExecutablePath, bool is_a, string sfen = null) {
            string move = null;
            while (move == null) {
                try {
                    move = askMove(botExecutablePath, is_a, sfen);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error asking bot {botExecutablePath} for move: {ex.Message}");
                    if (runningGames) {
                        // log the error
                        using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                            logWriter.WriteLine($"Error asking bot {botExecutablePath} for move");
                            logWriter.WriteLine($"FEN: {boardState.ToFEN()}");
                            logWriter.WriteLine($"Move history: {string.Join(" ", moveHistory)}");
                            logWriter.WriteLine();
                        }
                    }
                    ClearBotEngines();
                }
            }
            return move;
        }

        BotInstance GetOrStartBot(string botExecutablePath) {
            if (botInstances.TryGetValue(botExecutablePath, out var existing)) {
                return existing;
            }

            var botProcess = new Process();
            botProcess.StartInfo.FileName = botExecutablePath;
            botProcess.StartInfo.Arguments = "engine";
            botProcess.StartInfo.UseShellExecute = false;
            botProcess.StartInfo.RedirectStandardInput = true;
            botProcess.StartInfo.RedirectStandardOutput = true;
            botProcess.StartInfo.CreateNoWindow = true;
            botProcess.Start();

            var instance = new BotInstance {
                Process = botProcess,
                Input = botProcess.StandardInput,
                Output = botProcess.StandardOutput
            };

            botInstances[botExecutablePath] = instance;
            return instance;
        }

        void ClearBotEngines() {
            foreach (var kvp in botInstances) {
                var bot = kvp.Value;
                try {
                    bot.Input.WriteLine("end");
                    bot.Input.Flush();

                    if (!bot.Process.WaitForExit(1000)) {
                        bot.Process.Kill(true);
                        bot.Process.WaitForExit();
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error stopping bot {kvp.Key}: {ex.Message}");
                }
                finally {
                    bot.Process.Dispose();
                }
            }

            botInstances.Clear();
        }

        string convertEval(int eval) {
            if (eval > 1000000 - 1000) {
                return "Mate in " + (1000000 - eval);
            } else if (eval < -1000000 + 1000) {
                return "Mated in " + -(-1000000 - eval);
            } else {
                return eval.ToString();
            }
        }
        
        string convertDepth (int depth) {
            if (depth == 0) {
                return "Book";
            }
            return depth.ToString();
        }
    }
}