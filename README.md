# Chess Interface Manager

A Windows-based GUI for managing and analyzing chess games.

## Features

- **Move Generation:** Get moves from C-based chess engine implementation.
- **Modes:**
  - Human vs Human
  - Human vs Engine
  - Engine vs Engine
- **Batch Analysis:** Analyze multiple Engine vs Engine games with random, balanced FENs.

## Getting Started

1. **Requirements:**
   - .NET 7.0+ (or .NET 9.0 for latest builds)
   - Windows OS
   - C chess engine binary (see `assets/chess_implementation/chess.exe` for the one I made)

2. **Running the Interface:**
   - Build build the project using Visual Studio or `dotnet build`.
   - Ensure your engine binary is present at `assets/chess_implementation/chess.exe`.
   - Launch the interface and select your desired mode.

3. **FEN Analysis:**
   - Place FENs to analyze in `assets/fens/fens.txt` (one per line).
   - Use the batch analysis feature to run multiple games automatically.

## Assets
- **Engine:** Place your compiled engine in `assets/chess_implementation/`.
- **FENs:** Store FEN positions in `assets/fens/fens.txt`.
- **Piece Images:** Located in `assets/images/` for GUI rendering.

## Logging
- Game and analysis logs are saved in the `logs/` directory with timestamps.

## License
This project is for personal use. Adapt and extend as needed for your own chess engine projects.