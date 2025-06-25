using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ChessInterface {
    public class BotHandler {

        static Dictionary<string, BotInstance> botInstances = new();
        public static readonly string logFolder = "logs/";
        static readonly int thinkingTimeMs = 100; // Time in milliseconds for the bot to think

        public string currentLogFile = null;
        public string botExecutablePathA = null;
        public bool botASide = true;
        public string botExecutablePathB = null;
        public int selectedBotWhite = 0;
        public int selectedBotBlack = 0;
        public int numGames = 500;
        public bool runningGames = false;
        public int wins = 0, draws = 0, losses = 0, gamesPlayed = 0;

        public int botALastDepth = 0;
        public int botBLastDepth = 0;
        public int botALastEval = 0;
        public int botBLastEval = 0;

        public bool thinking = false;

        public BotHandler() { }
        
        public string askMove(string botExecutablePath, bool is_a, string sfen = null) {
            sfen ??= ChessApp.startFEN;

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
                logWriter.WriteLine($"FEN: {ChessApp.instance.boardState.ToFEN()}");
                logWriter.WriteLine($"Move history: {string.Join(" ", ChessApp.instance.moveHistory)}");
            }

            // Set FEN
            Send("setfen");
            Send(sfen);
            if (Receive() != "ok") return null;

            // Move history
            Send("setmovehistory");
            Send(string.Join(" ", ChessApp.instance.moveHistory));
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

        public void HandleRunningGames() {
            bool white_to_move = ChessApp.instance.boardState.white_to_move;
            if (runningGames) {
                // If the engine is running, ask it for a move
                if (!thinking) {
                    thinking = true;
                    string move = askMoveTryAgain(white_to_move == botASide ? botExecutablePathA : botExecutablePathB, white_to_move == botASide, ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count]);
                    if (move != null) {
                        // Add the move to the history
                        ChessApp.instance.moveHistory.Add(move);
                        ChessApp.instance.UpdateBoardState(ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count], true);

                        if (ChessApp.instance.state == State.CheckMate) {
                            losses += botASide == white_to_move ? 1 : 0;
                            wins += botASide == white_to_move ? 0 : 1;
                        } else if (ChessApp.instance.state != State.None) {
                            draws++;
                        }

                        if (ChessApp.instance.state != State.None && gamesPlayed < numGames) {
                            if (++gamesPlayed >= numGames) {
                                runningGames = false;
                                // save to log file
                            }
                            using (StreamWriter logWriter = new(currentLogFile, append: true)) {
                                logWriter.WriteLine($"=======================");
                                logWriter.WriteLine($"  Result Game {gamesPlayed}");
                                logWriter.WriteLine($"  Result: {(ChessApp.instance.state == State.CheckMate ? (
                                    (botASide == white_to_move ? "Bot B wins" : "Bot A wins")
                                    + (white_to_move ? " as black" : " as white")
                                ) : "Draw")}");
                                logWriter.WriteLine($"  Ending FEN: {ChessApp.instance.boardState.ToFEN()}");
                                logWriter.WriteLine($"=======================");
                                logWriter.WriteLine();

                                if (runningGames) {
                                    logWriter.WriteLine($"=======================");
                                    logWriter.WriteLine($"  Game {gamesPlayed + 1}");
                                    logWriter.WriteLine($"  Starting FEN: {ChessApp.instance.fens[++ChessApp.instance.fen_index % ChessApp.instance.fens.Count]}");
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

                            ChessApp.instance.boardState.FromFEN(ChessApp.instance.fens[ChessApp.instance.fen_index % ChessApp.instance.fens.Count]);
                            ChessApp.instance.moveHistory.Clear();
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

        public string askMoveTryAgain(string botExecutablePath, bool is_a, string sfen = null) {
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
                            logWriter.WriteLine($"FEN: {ChessApp.instance.boardState.ToFEN()}");
                            logWriter.WriteLine($"Move history: {string.Join(" ", ChessApp.instance.moveHistory)}");
                            logWriter.WriteLine();
                        }
                    }
                    ClearBotEngines();
                }
            }
            return move;
        }

        public BotInstance GetOrStartBot(string botExecutablePath) {
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

        public void ClearBotEngines() {
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
    }
}
