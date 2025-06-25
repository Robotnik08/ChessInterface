namespace ChessInterface {
    public class BoardState {
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
            board.Reverse();
            string fen = "";
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
            board.Reverse();
            return fen;
        }

        public void FromFEN(string fen) {
            Console.WriteLine($"Parsing FEN: {fen}");
            string[] parts = fen.Split(' ');
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
            white_to_move = parts[1] == "w";
            castling_rights_white_king_side = parts[2].Contains('K');
            castling_rights_white_queen_side = parts[2].Contains('Q');
            castling_rights_black_king_side = parts[2].Contains('k');
            castling_rights_black_queen_side = parts[2].Contains('q');
            en_passant_target_square = parts[3] == "-" ? -1 : (parts[3][0] - 'a') + ((8 - (parts[3][1] - '0')) * 8);
            halfmove_clock = int.Parse(parts[4]);
            fullmove_number = int.Parse(parts[5]);
            board.Reverse();
        }

        public BoardState(string fen) {
            FromFEN(fen);
        }
    }

    public enum State {
        None,
        CheckMate,
        StaleMate,
        ThreefoldRepetition,
        FiftyMoveRule,
        InsufficientMaterial,
    }
}
