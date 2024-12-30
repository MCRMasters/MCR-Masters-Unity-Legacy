using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    // Enums
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
        NORTH = 30
    }

    public enum TileType
    {
        MANZU = 0,
        PINZU = 1,
        SOUZU = 2,
        HONOR = 3,
        FLOWER = 4
    }

    // Block class
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

    // WinningCondition class
    public class WinningCondition
    {
        public int WinningTile;
        public bool IsDiscarded;
        public bool IsLastTileInTheGame;
        public bool IsLastTileOfItsKind;
        public bool IsReplacementTile;
        public bool IsRobbingTheKong;
        public int CountWinningConditions;

        public WinningCondition() { }

        public WinningCondition(int winningTile, bool isDiscarded, bool isLastTileInTheGame, bool isLastTileOfItsKind, bool isReplacementTile, bool isRobbingTheKong, int countWinningConditions)
        {
            WinningTile = winningTile;
            IsDiscarded = isDiscarded;
            IsLastTileInTheGame = isLastTileInTheGame;
            IsLastTileOfItsKind = isLastTileOfItsKind;
            IsReplacementTile = isReplacementTile;
            IsRobbingTheKong = isRobbingTheKong;
            CountWinningConditions = countWinningConditions;
        }
    }

    // Hand class
    public class Hand
    {
        public List<int> ClosedTiles = new List<int>();
        public List<int> OpenedTiles = new List<int>();
        public List<Block> CallBlocks = new List<Block>();
        public int FlowerPoint;
        public int WinningTile;
        public bool IsBlocksDivided;
        public List<KeyValuePair<int, int>> YakuScoreList = new List<KeyValuePair<int, int>>();
        public int HighestScore;
        public List<int> KeishikiTenpaiTiles = new List<int>();
    }

    // PlayerManager fields
    [SyncVar]
    public int PlayerIndex;

    [SyncVar]
    public string PlayerName;

    [SyncVar]
    public int CurrentScore;

    [SyncVar]
    public int SeatWind;

    [SyncVar]
    public int RoundWind;

    [SyncVar]
    public bool IsPlayerTurn;

    public Hand PlayerHand = new Hand();
    public WinningCondition PlayerWinningCondition = new WinningCondition();
    public List<int> PlayerKawaTiles = new List<int>();

    public Dictionary<int, string> TileToString = new Dictionary<int, string>();
    public Dictionary<string, int> StringToTile = new Dictionary<string, int>();

    private void Start()
    {
        if (isServer)
        {
            InitializeTileDictionaries();
        }
    }

    private void InitializeTileDictionaries()
    {
        for (int i = 0; i < 34; i++)
        {
            string tileName = TileNumToString(i);
            TileToString[i] = tileName;
            StringToTile[tileName] = i;
        }
        TileToString[34] = "0f";
        StringToTile["0f"] = 34;
    }

    private string TileNumToString(int tileNum)
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

    public bool CheckIfPlayerTurn()
    {
        return IsPlayerTurn;
    }
}
