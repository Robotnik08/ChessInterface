using System;
using Raylib_cs;
using System.Collections.Generic;
using System.Diagnostics;

using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Texture2D = Raylib_cs.Texture2D;
using Rectangle = Raylib_cs.Rectangle;

namespace ChessInterface {
    public class ChessApp {

        public static ChessApp instance = null;

        public static readonly string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        
        static readonly string startMoves = "";
        static readonly string chessImplementationPath = "assets/chess_implementation/chess.exe";
        static readonly string fensLocation = "assets/fens/fens.txt";

        static readonly int board_size = 800;
        static readonly int uiWidth = 400;

        public BoardState boardState = new();
        public State state = State.None;

        public List<string> moves = [];
        public List<string> moveHistory;

        public List<string> fens = [];
        public int fen_index = 0;

        public int holdIndex = -1;
        public bool isDragging = false;
        public char promotionPiece = 'q';


        public BoardDrawer boardDrawer;
        public UIDrawer uiDrawer;
        public BotHandler botHandler;


        public ChessApp() {
            if (instance != null) {
                throw new Exception("ChessApp instance already exists.");
            }
            instance = this;

            moveHistory = string.IsNullOrWhiteSpace(startMoves) ? [] : startMoves.Split(' ').ToList();

            fens = [.. File.ReadAllLines(fensLocation)];

            boardState.FromFEN(startFEN);
            state = State.None;


            int windowWidth = board_size + uiWidth;
            int windowHeight = board_size;

            Raylib.InitWindow(windowWidth, windowHeight, "Chess interface - version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Raylib.SetTargetFPS(166);

            boardDrawer = new();
            uiDrawer = new();
            botHandler = new();

            Image iconTexture = Raylib.LoadImage("assets/images/bp.png");
            Raylib.SetWindowIcon(iconTexture);


            UpdateBoardState();

            // Main game loop
            MainLoop();

            Raylib.CloseWindow();
        }

        public void MainLoop() {
            while (!Raylib.WindowShouldClose()) {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.RayWhite);

                uiDrawer.DrawUIAndHandleInteraction(uiWidth, board_size);
                boardDrawer.DrawBoard(board_size, uiWidth, 0);
                boardDrawer.HandleBoardInteraction(board_size, uiWidth, 0);

                Raylib.EndDrawing();

                HandleKeyBinds();

                botHandler.HandleRunningGames();
            }
        }

        public void HandleKeyBinds() {
            bool runningGames = botHandler.runningGames;

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
                    botHandler.ClearBotEngines();
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.M) && !runningGames) {
                // force the bot to make a move
                if (!runningGames) {
                    string move = botHandler.askMoveTryAgain(boardState.white_to_move == botHandler.botASide ? botHandler.botExecutablePathA : botHandler.botExecutablePathB, boardState.white_to_move == botHandler.botASide);
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

            if (Raylib.IsKeyPressed(KeyboardKey.Right) && !runningGames) {
                // Next FEN
                fen_index = (fen_index + 1) % fens.Count;
                string nextFEN = fens[fen_index];
                boardState.FromFEN(nextFEN);
                moveHistory.Clear();
                UpdateBoardState(nextFEN);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Left) && !runningGames) {
                // Previous FEN
                fen_index = (fen_index - 1 + fens.Count) % fens.Count;
                string prevFEN = fens[fen_index];
                boardState.FromFEN(prevFEN);
                moveHistory.Clear();
                UpdateBoardState(prevFEN);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.L) && !runningGames) {
                // Load a FEN from a message box
                string inputFEN = Microsoft.VisualBasic.Interaction.InputBox("Enter FEN string:", "Load FEN", boardState.ToFEN());
                if (!string.IsNullOrWhiteSpace(inputFEN)) {
                    boardState.FromFEN(inputFEN);
                    moveHistory.Clear();
                    UpdateBoardState(inputFEN);
                }
            }
        }
        
        public void UpdateBoardState(string sfen = null, bool is_bot_vs_bot = false) {
            sfen ??= startFEN;

            Process engineProcess = new();
            engineProcess.StartInfo.FileName = chessImplementationPath;
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

            if (!is_bot_vs_bot) {
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
    }
}