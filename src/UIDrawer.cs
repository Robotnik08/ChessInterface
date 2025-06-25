using System;
using Raylib_cs;
using System.Numerics;

using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Texture2D = Raylib_cs.Texture2D;
using Rectangle = Raylib_cs.Rectangle;

namespace ChessInterface {
    public class UIDrawer {

        private string[] botOptions = ["Human", "Select"];
        public UIDrawer() { }

        public void DrawUIAndHandleInteraction(int uiWidth, int windowHeight) {
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
                Raylib.DrawRectangleRec(btn, i == ChessApp.instance.botHandler.selectedBotWhite ? Color.LightGray : Color.Gray);
                Raylib.DrawText(botOptions[i], (int)btn.X + 8, (int)btn.Y + 4, 18, Color.Black);
                if (i == 1) {
                    Raylib.DrawText((ChessApp.instance.botHandler.botExecutablePathA ?? "No bot selected").Split("\\").Last(), (int)btn.X + 8 + 80, (int)btn.Y + 4, 18, Color.Red);
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btn) && !ChessApp.instance.botHandler.runningGames) {
                    if (i == 1) {
                        // Open file dialog to select bot executable
                        OpenFileDialog openFileDialog = new() {
                            Filter = "Executable files (*.exe)|*.exe",
                            Title = "Select Bot Executable"
                        };
                        if (openFileDialog.ShowDialog() == DialogResult.OK) {
                            ChessApp.instance.botHandler.botExecutablePathA = openFileDialog.FileName;
                            ChessApp.instance.botHandler.selectedBotWhite = i;
                            if (ChessApp.instance.botHandler.selectedBotBlack == 0) {
                                // make the first move against the human
                                ChessApp.instance.boardState.FromFEN(ChessApp.startFEN);
                                ChessApp.instance.moveHistory.Clear();
                                ChessApp.instance.botHandler.botASide = true; // A bot is white
                                string move = ChessApp.instance.botHandler.askMoveTryAgain(ChessApp.instance.botHandler.botExecutablePathA, true);

                                ChessApp.instance.moveHistory.Add(move);
                                ChessApp.instance.UpdateBoardState();
                            }
                        } else {
                            ChessApp.instance.botHandler.botExecutablePathA = null;
                        }
                    } else {
                        ChessApp.instance.botHandler.selectedBotWhite = i;
                        ChessApp.instance.botHandler.botExecutablePathA = null;
                    }
                }
            }
            pos_y += botOptions.Length * 28 + 10;

            // B bot dropdown
            Raylib.DrawText("B:", panelX + 20, pos_y, 20, Color.Black);
            for (int i = 0; i < botOptions.Length; i++) {
                Rectangle btn = new(panelX + 90, pos_y + i * 28, 80, 24);
                Raylib.DrawRectangleRec(btn, i == ChessApp.instance.botHandler.selectedBotBlack ? Color.LightGray : Color.Gray);
                Raylib.DrawText(botOptions[i], (int)btn.X + 8, (int)btn.Y + 4, 18, Color.Black);
                if (i == 1) {
                    Raylib.DrawText((ChessApp.instance.botHandler.botExecutablePathB ?? "No bot selected").Split("\\").Last(), (int)btn.X + 8 + 80, (int)btn.Y + 4, 18, Color.Red);
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btn) && !ChessApp.instance.botHandler.runningGames) {
                    if (i == 1) {
                        // Open file dialog to select bot executable
                        OpenFileDialog openFileDialog = new() {
                            Filter = "Executable files (*.exe)|*.exe",
                            Title = "Select Bot Executable"
                        };
                        if (openFileDialog.ShowDialog() == DialogResult.OK) {
                            ChessApp.instance.botHandler.selectedBotBlack = i;
                            ChessApp.instance.botHandler.botExecutablePathB = openFileDialog.FileName;
                        } else {
                            ChessApp.instance.botHandler.botExecutablePathB = null;
                        }
                    } else {
                        ChessApp.instance.botHandler.selectedBotBlack = i;
                        ChessApp.instance.botHandler.botExecutablePathB = null;
                    }
                }
            }
            pos_y += botOptions.Length * 28 + 10;

            // Number of games input
            Raylib.DrawText("Games to test:", panelX + 20, pos_y, 20, Color.Black);
            Rectangle numGamesBox = new(panelX + 160, pos_y, 60, 28);
            Raylib.DrawRectangleRec(numGamesBox, Color.White);
            Raylib.DrawRectangleLines((int)numGamesBox.X, (int)numGamesBox.Y, (int)numGamesBox.Width, (int)numGamesBox.Height, Color.Black);
            Raylib.DrawText(ChessApp.instance.botHandler.numGames.ToString(), (int)numGamesBox.X + 8, (int)numGamesBox.Y + 4, 20, Color.Black);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), numGamesBox)) {
                ChessApp.instance.botHandler.numGames = (ChessApp.instance.botHandler.numGames % 1000) + 10;
            } else if (Raylib.IsMouseButtonPressed(MouseButton.Right) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), numGamesBox)) {
                ChessApp.instance.botHandler.numGames = (ChessApp.instance.botHandler.numGames - 20 + 1000) % 1000 + 10; // Wrap around to keep it positive
            }
            pos_y += spacing;

            // Results bar
            Raylib.DrawText("Results:", panelX + 20, pos_y, 20, Color.Black);
            pos_y += 28;
            int barW = panelW - 40;
            int barH = 28;
            int total = ChessApp.instance.botHandler.wins + ChessApp.instance.botHandler.draws + ChessApp.instance.botHandler.losses;
            float winFrac = total > 0 ? (float)ChessApp.instance.botHandler.wins / total : 0;
            float drawFrac = total > 0 ? (float)ChessApp.instance.botHandler.draws / total : 0;
            float lossFrac = total > 0 ? (float)ChessApp.instance.botHandler.losses / total : 0;
            int winW = (int)(barW * winFrac);
            int drawW = (int)(barW * drawFrac);
            int lossW = barW - winW - drawW;
            int barX = panelX + 20;
            int barY = pos_y;
            Raylib.DrawRectangle(barX, barY, winW, barH, Color.Green);
            Raylib.DrawRectangle(barX + winW, barY, drawW, barH, Color.Yellow);
            Raylib.DrawRectangle(barX + winW + drawW, barY, lossW, barH, Color.Red);
            Raylib.DrawRectangleLines(barX, barY, barW, barH, Color.Black);
            Raylib.DrawText($"W: {ChessApp.instance.botHandler.wins}  D: {ChessApp.instance.botHandler.draws}  L: {ChessApp.instance.botHandler.losses}", barX + 5, barY + 4, 18, Color.Black);
            pos_y += barH + 20;

            // Start/Stop buttons
            Rectangle startBtn = new(panelX + 20, pos_y, 120, 36);
            Rectangle stopBtn = new(panelX + 160, pos_y, 120, 36);
            Raylib.DrawRectangleRec(startBtn, ChessApp.instance.botHandler.runningGames ? Color.Gray : Color.SkyBlue);
            Raylib.DrawText("Start", (int)startBtn.X + 30, (int)startBtn.Y + 8, 22, Color.Black);
            Raylib.DrawRectangleRec(stopBtn, ChessApp.instance.botHandler.runningGames ? Color.SkyBlue : Color.Gray);
            Raylib.DrawText("Stop", (int)stopBtn.X + 35, (int)stopBtn.Y + 8, 22, Color.Black);

            // when games are running, say which side is which bot
            if (ChessApp.instance.botHandler.runningGames) {
                Raylib.DrawText($"Bot A: {(ChessApp.instance.botHandler.botASide ? "White" : "Black")}", panelX + 20, pos_y + 50, 20, Color.Black);
                Raylib.DrawText($"Bot B: {(!ChessApp.instance.botHandler.botASide ? "White" : "Black")}", panelX + 20, pos_y + 80, 20, Color.Black);

                // also draw the last depth and eval of the bots
                Raylib.DrawText($"Bot A Depth: {convertDepth(ChessApp.instance.botHandler.botALastDepth)}, Eval: {convertEval(ChessApp.instance.botHandler.botALastEval)}", panelX + 20, pos_y + 110, 20, Color.Black);
                Raylib.DrawText($"Bot B Depth: {convertDepth(ChessApp.instance.botHandler.botBLastDepth)}, Eval: {convertEval(ChessApp.instance.botHandler.botBLastEval)}", panelX + 20, pos_y + 140, 20, Color.Black);
            }

            do {
                if (!ChessApp.instance.botHandler.runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), startBtn)) {
                    if (ChessApp.instance.botHandler.selectedBotWhite == 0 || ChessApp.instance.botHandler.selectedBotBlack == 0) {
                        MessageBox.Show("Please select both bots before starting the games.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }

                    ChessApp.instance.botHandler.runningGames = true;
                    ChessApp.instance.botHandler.wins = ChessApp.instance.botHandler.draws = ChessApp.instance.botHandler.losses = 0;
                    ChessApp.instance.botHandler.gamesPlayed = 0;
                    ChessApp.instance.fen_index = 0;
                    ChessApp.instance.botHandler.ClearBotEngines();

                    ChessApp.instance.boardState.FromFEN(ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count]);
                    ChessApp.instance.moveHistory.Clear();
                    ChessApp.instance.botHandler.botASide = true; // Start with Bot A as white

                    ChessApp.instance.UpdateBoardState(ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count]);

                    // append to log file	
                    ChessApp.instance.botHandler.currentLogFile = $"{BotHandler.logFolder}chess_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                    // Create the log file if it doesn't exist
                    if (!System.IO.Directory.Exists(BotHandler.logFolder)) {
                        System.IO.Directory.CreateDirectory(BotHandler.logFolder);
                    }

                    using (StreamWriter logWriter = new(ChessApp.instance.botHandler.currentLogFile, append: true)) {
                        logWriter.WriteLine($"==============================================");
                        logWriter.WriteLine($"\tStarting games at {DateTime.Now}");
                        logWriter.WriteLine($"\tBot A: {ChessApp.instance.botHandler.botExecutablePathA ?? "Human"}");
                        logWriter.WriteLine($"\tBot B: {ChessApp.instance.botHandler.botExecutablePathB ?? "Human"}");
                        logWriter.WriteLine($"\tNumber of games: {ChessApp.instance.botHandler.numGames}");
                        logWriter.WriteLine($"==============================================");
                        logWriter.WriteLine();


                        logWriter.WriteLine($"=======================");
                        logWriter.WriteLine($"  Game {ChessApp.instance.botHandler.gamesPlayed + 1}");
                        logWriter.WriteLine($"  Starting FEN: {ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count]}");
                        logWriter.WriteLine($"  White: {(ChessApp.instance.botHandler.botASide ? "Bot A" : "Bot B")}");
                        logWriter.WriteLine($"  Black: {(ChessApp.instance.botHandler.botASide ? "Bot B" : "Bot A")}");
                        logWriter.WriteLine($"=======================");
                        logWriter.WriteLine();
                    }
                }
            } while (false);


            if (ChessApp.instance.botHandler.runningGames && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), stopBtn)) {
                    ChessApp.instance.botHandler.runningGames = false;
                }
        }
        
        private string convertEval(int eval) {
            if (eval > 1000000 - 1000) {
                return "Mate in " + (1000000 - eval);
            } else if (eval < -1000000 + 1000) {
                return "Mated in " + -(-1000000 - eval);
            } else {
                return eval.ToString();
            }
        }
        
        private string convertDepth (int depth) {
            if (depth == 0) {
                return "Book";
            }
            return depth.ToString();
        }
    }
}
