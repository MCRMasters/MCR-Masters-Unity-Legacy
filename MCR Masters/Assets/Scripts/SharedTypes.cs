using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared
{

    public static class TileDictionary
    {
        public static readonly Dictionary<int, string> NumToString = new Dictionary<int, string>();
        public static readonly Dictionary<string, int> StringToNum = new Dictionary<string, int>();

        static TileDictionary()
        {
            for (int i = 0; i < 34; i++)
            {
                string tileName = TileNumToString(i);
                NumToString[i] = tileName;
                StringToNum[tileName] = i;
            }
            NumToString[34] = "0f";
            StringToNum["0f"] = 34;
        }

        private static string TileNumToString(int tileNum)
        {
            string tileName = "";
            if (tileNum >= 0 && tileNum < 34)
            {
                tileName += (char)('1' + tileNum % 9);
                if (tileNum < 9) tileName += "m";
                else if (tileNum < 18) tileName += "p";
                else if (tileNum < 27) tileName += "s";
                else tileName += "z";
            }
            else if (tileNum == 34)
            {
                tileName = "0f";
            }
            return tileName;
        }
    }

    public class PlayerStatus
    {
        public int CurrentScore { get; set; }
        public Wind SeatWind { get; set; }
        public Wind RoundWind { get; set; }
        public bool IsPlayerTurn { get; set; }

        public PlayerStatus()
        {
            CurrentScore = 0;
            SeatWind = Wind.EAST;
            RoundWind = Wind.EAST;
            IsPlayerTurn = false;
        }

        public PlayerStatus(Wind seatWind, Wind roundWind)
        {
            CurrentScore = 0;
            SeatWind = seatWind;
            RoundWind = roundWind;
            IsPlayerTurn = (seatWind == Wind.EAST);
        }

        public override string ToString()
        {
            return $"Score: {CurrentScore}, SeatWind: {SeatWind}, RoundWind: {RoundWind}, IsPlayerTurn: {IsPlayerTurn}";
        }
    }

    public enum BlockType
    {
        SEQUENCE,
        TRIPLET,
        QUAD,
        KNITTED,
        PAIR,
        SINGLETILE
    }

    public enum BlockSource
    {
        SELF,
        SHIMOCHA,
        TOIMEN,
        KAMICHA
    }

    public enum Wind
    {
        EAST = 27,
        SOUTH = 28,
        WEST = 29,
        NORTH = 30,
        END = 31
    }

    public enum TileType
    {
        MANZU = 0,
        PINZU = 1,
        SOUZU = 2,
        HONOR = 3,
        FLOWER = 4
    }

    public class Block
    {
        public BlockType Type;
        public int Tile;
        public BlockSource Source;
        public int SourceTileIndex;

        public Block() { }

        public Block(BlockType type, int tile)
        {
            Type = type;
            Tile = tile;
            Source = BlockSource.SELF;
            SourceTileIndex = 0;
        }

        public Block(BlockType type, int tile, BlockSource source, int sourceTileIndex = 0)
        {
            Type = type;
            Tile = tile;
            Source = source;
            SourceTileIndex = sourceTileIndex;
        }
    }

    public class WinningCondition
    {
        public int WinningTile;
        public bool IsDiscarded;
        public bool IsLastTileInTheGame;
        public bool IsLastTileOfItsKind;
        public bool IsReplacementTile;
        public bool IsRobbingTheKong;
        public int CountWinningTiles;

        public WinningCondition() { }

        public WinningCondition(int winningTile, bool isDiscarded, bool isLastTileInTheGame, bool isLastTileOfItsKind, bool isReplacementTile, bool isRobbingTheKong, int countWinningTiles)
        {
            WinningTile = winningTile;
            IsDiscarded = isDiscarded;
            IsLastTileInTheGame = isLastTileInTheGame;
            IsLastTileOfItsKind = isLastTileOfItsKind;
            IsReplacementTile = isReplacementTile;
            IsRobbingTheKong = isRobbingTheKong;
            CountWinningTiles = countWinningTiles;
        }
    }

    public class Hand
    {
        public List<int> ClosedTiles { get; private set; }
        public List<int> OpenedTiles { get; private set; }
        public List<Block> CallBlocks { get; private set; }
        public int FlowerPoint { get; set; }
        public int WinningTile { get; set; }
        public List<KeyValuePair<int, int>> YakuScoreList { get; private set; }
        public int HighestScore { get; set; }
        public List<int> KeishikiTenpaiTiles { get; private set; }
        private int TilesLeftToDraw { get; set; }
        public Hand()
        {
            Initialize();
        }

        private bool TileOutOfRange(int tile)
        {
            return tile < 0 || tile >= 35;
        }
        private void Initialize()
        {
            ClosedTiles = new List<int>(new int[35]); // 초기값 0으로 35개 생성
            OpenedTiles = new List<int>(new int[35]); // 초기값 0으로 35개 생성
            CallBlocks = new List<Block>();
            YakuScoreList = new List<KeyValuePair<int, int>>();
            KeishikiTenpaiTiles = new List<int>();
            FlowerPoint = 0;
            WinningTile = -1;
            HighestScore = 0;
            TilesLeftToDraw = 14;
        }

        public int DrawFirstHand(List<int> hand)
        {
            Debug.Log($"[DrawFirstHand] Attempting to draw hand: {string.Join(", ", hand.Select(t => TileDictionary.NumToString[t]))}");

            if (hand == null || hand.Count != 13 || TilesLeftToDraw != 14)
            {
                Debug.LogError("[DrawFirstHand] Invalid hand or incorrect draw state.");
                return -1;
            }
            for (int i = 0; i < hand.Count; i++)
            {
                if (TileOutOfRange(hand[i]))
                {
                    Debug.LogError($"[DrawFirstHand] Tile {hand[i]} is out of range.");
                    return -1;
                }
                ClosedTiles[hand[i]] += 1;
                TilesLeftToDraw -= 1;
            }

            Debug.Log("[DrawFirstHand] Successfully drew first hand.");
            PrintHand();
            PrintHandNames();
            return 0;
        }

        public int DiscardOneTile(int tile)
        {
            Debug.Log($"[DiscardOneTile] Attempting to discard tile: {TileDictionary.NumToString[tile]}");

            if (TilesLeftToDraw != 0 || TileOutOfRange(tile) || ClosedTiles[tile] <= 0)
            {
                Debug.LogError("[DiscardOneTile] Invalid discard operation.");
                return -1;
            }
            ClosedTiles[tile] -= 1;
            TilesLeftToDraw += 1;
            WinningTile = -1;

            Debug.Log("[DiscardOneTile] Successfully discarded tile.");
            PrintHand();
            PrintHandNames();
            return 0;
        }

        public int TsumoOneTile(int tile)
        {
            Debug.Log($"[TsumoOneTile] Attempting to tsumo tile: {TileDictionary.NumToString[tile]}");

            if (TilesLeftToDraw != 1 || TileOutOfRange(tile))
            {
                Debug.LogError("[TsumoOneTile] Invalid tsumo operation.");
                return -1;
            }
            ClosedTiles[tile] += 1;
            TilesLeftToDraw -= 1;
            WinningTile = tile;

            Debug.Log("[TsumoOneTile] Successfully tsumoed tile.");
            PrintHand();
            PrintHandNames();
            return 0;
        }

        private void PrintHand()
        {
            var closedTilesOutput = string.Join(" ", ClosedTiles.Select((value, index) => index % 9 == 0 && index != 0 ? $"\n{value}" : value.ToString()));
            var openedTilesOutput = string.Join(" ", OpenedTiles.Select((value, index) => index % 9 == 0 && index != 0 ? $"\n{value}" : value.ToString()));
            Debug.Log($"[PrintHand] Closed Tiles:\n{closedTilesOutput}\n[PrintHand] Opened Tiles:\n{openedTilesOutput}\nFlower Points: {FlowerPoint}");
        }

        private void PrintHandNames()
        {
            var closedTilesNames = string.Join(" ", ClosedTiles.SelectMany((value, index) => Enumerable.Repeat(TileDictionary.NumToString[index], value)));
            var openedTilesNames = string.Join(" ", OpenedTiles.SelectMany((value, index) => Enumerable.Repeat(TileDictionary.NumToString[index], value)));
            var winningTileName = WinningTile >= 0 && WinningTile < TileDictionary.NumToString.Count ? TileDictionary.NumToString[WinningTile] : "None";

            Debug.Log($"[PrintHandNames] Closed Tiles: {closedTilesNames}\n[PrintHandNames] Opened Tiles: {openedTilesNames}\nFlower Points: {FlowerPoint}\nWinning Tile: {winningTileName}");
        }

    }
}
