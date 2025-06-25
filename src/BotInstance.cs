using System.Diagnostics;
using System.IO;

namespace ChessInterface {
    public class BotInstance {
        public Process Process { get; set; }
        public StreamWriter Input { get; set; }
        public StreamReader Output { get; set; }
    }
}
