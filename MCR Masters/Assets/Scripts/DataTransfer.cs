using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Game.Shared;
using UnityEngine;

namespace DataTransfer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TileScoreData
    {
        public int Tile;       // 타일 번호
        public int TsumoScore; // 쯔모 점수
        public int RonScore;   // 론 점수
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TileScoreDataArray
    {
        public IntPtr Data;    // TileScoreData* 포인터
        public int Count;      // 데이터 개수
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct YakuScoreData
    {
        public int YakuId;
        public int Score;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct YakuScoreDataArray
    {
        public IntPtr Data;
        public int Count;
    }

    // BlockData 구조체
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockData
    {
        public int Type;               // BlockType (enum -> int)
        public int Tile;               // 타일 ID
        public int Source;             // BlockSource (enum -> int)
        public int SourceTileIndex;    // 출처 타일의 인덱스

        public BlockData DeepCopy()
        {
            return new BlockData
            {
                Type = this.Type,
                Tile = this.Tile,
                Source = this.Source,
                SourceTileIndex = this.SourceTileIndex
            };
        }


        public override string ToString()
        {
            string result = "";

            if ((BlockSource)Source != BlockSource.SELF)
                result += "[";
            else if ((BlockType)Type == BlockType.QUAD)
                result += "{";

            switch ((BlockType)Type)
            {
                case BlockType.PAIR:
                    result += $"{TileDictionary.NumToString[Tile]}{TileDictionary.NumToString[Tile]}";
                    break;

                case BlockType.SEQUENCE:
                    for (int i = 0; i < 3; i++)
                        result += TileDictionary.NumToString[Tile + i];
                    break;

                case BlockType.TRIPLET:
                    for (int i = 0; i < 3; i++)
                        result += TileDictionary.NumToString[Tile];
                    break;

                case BlockType.QUAD:
                    for (int i = 0; i < 4; i++)
                        result += TileDictionary.NumToString[Tile];
                    break;

                case BlockType.SINGLETILE:
                    result += TileDictionary.NumToString[Tile];
                    break;

                case BlockType.KNITTED:
                    for (int i = 0; i < 3; i++)
                        result += TileDictionary.NumToString[Tile + i * 3];
                    break;

                default:
                    result += $"{(BlockType)Type} {TileDictionary.NumToString[Tile]} {(BlockSource)Source}";
                    break;
            }

            if ((BlockSource)Source != BlockSource.SELF)
                result += "]";
            else if ((BlockType)Type == BlockType.QUAD)
                result += "}";

            return result;
        }
    }

    // WinningConditionData 구조체
    [StructLayout(LayoutKind.Sequential)]
    public struct WinningConditionData
    {
        public int WinningTile;             // Winning 타일 ID
        public int IsDiscarded;            // 버려진 타일 여부
        public int IsLastTileInTheGame;    // 마지막 타일 여부
        public int IsLastTileOfItsKind;    // 해당 종류의 마지막 타일 여부
        public int IsReplacementTile;      // 대체 타일 여부
        public int IsRobbingTheKong;       // Robbing the Kong 여부
        public int RoundWind;
        public int SeatWind;

        public override string ToString()
        {
            return $"WinningTile: {WinningTile}, IsDiscarded: {IsDiscarded}, IsLastTileInTheGame: {IsLastTileInTheGame}, " +
                   $"IsLastTileOfItsKind: {IsLastTileOfItsKind}, IsReplacementTile: {IsReplacementTile}, IsRobbingTheKong: {IsRobbingTheKong}, " +
                   $"RoundWind: {RoundWind}, SeatWind: {SeatWind}";
        }
    }

    // HandData 구조체
    [StructLayout(LayoutKind.Sequential)]
    public struct HandData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
        public int[] ClosedTiles;           // 숨겨진 손패 (고정 크기 배열)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
        public int[] OpenedTiles;           // 드러난 손패 (고정 크기 배열)
        public int WinningTile;             // Winning 타일 ID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public BlockData[] CallBlocks;      // 블록 데이터 (최대 14개)

        public int CallBlockCount;          // 실제 CallBlocks의 개수

        public HandData DeepCopy()
        {
            HandData copy = new HandData
            {
                // ClosedTiles 배열 깊은 복사
                ClosedTiles = (int[])this.ClosedTiles.Clone(),

                // OpenedTiles 배열 깊은 복사
                OpenedTiles = (int[])this.OpenedTiles.Clone(),

                // WinningTile은 값형이므로 그대로 복사
                WinningTile = this.WinningTile,

                // CallBlocks 배열 깊은 복사
                CallBlocks = this.CallBlocks != null
                    ? this.CallBlocks.Select(cb => cb.DeepCopy()).ToArray()
                    : new BlockData[14],

                // CallBlockCount 값 복사
                CallBlockCount = this.CallBlockCount
            };

            return copy;
        }


        public void PrintHand()
        {
            var closedTilesOutput = string.Join(" ",
                ClosedTiles.Select((value, index) => index % 9 == 0 && index != 0 ? $"\n{value}" : value.ToString()));

            var openedTilesOutput = string.Join(" ",
                OpenedTiles.Select((value, index) => index % 9 == 0 && index != 0 ? $"\n{value}" : value.ToString()));

            Debug.Log($"[PrintHand] Closed Tiles:\n{closedTilesOutput}\n[PrintHand] Opened Tiles:\n{openedTilesOutput}");
            PrintCallBlocks();
        }

        public void PrintHandNames()
        {
            var closedTilesNames = string.Join(" ",
                ClosedTiles.SelectMany((value, index) => Enumerable.Repeat(TileDictionary.NumToString[index], value)));

            var openedTilesNames = string.Join(" ",
                OpenedTiles.SelectMany((value, index) => Enumerable.Repeat(TileDictionary.NumToString[index], value)));

            var winningTileName = WinningTile >= 0 && WinningTile < TileDictionary.NumToString.Count
                ? TileDictionary.NumToString[WinningTile]
                : "None";

            Debug.Log($"[PrintHandNames] Closed Tiles: {closedTilesNames}\nOpened Tiles: {openedTilesNames}\nWinning Tile: {winningTileName}");
            PrintCallBlocks();
        }

        public void PrintCallBlocks()
        {
            if (CallBlocks == null || CallBlockCount == 0)
            {
                Debug.Log("CallBlocks가 비어있습니다.");
                return;
            }

            Debug.Log("CallBlocks 출력:");
            for (int i = 0; i < CallBlockCount; i++)
            {
                Debug.Log(CallBlocks[i].ToString());
            }
        }

        public override string ToString()
        {
            string closedTiles = string.Join(", ", ClosedTiles);
            string openedTiles = string.Join(", ", OpenedTiles);
            string callBlocks = string.Join(", ", CallBlocks?.Select(cb => cb.ToString()) ?? new string[0]);

            return $"ClosedTiles: [{closedTiles}], OpenedTiles: [{openedTiles}], WinningTile: {WinningTile}, " +
                   $"CallBlocks: [{callBlocks}], CallBlockCount: {CallBlockCount}";
        }
    }

    public static class HandConverter
    {
        public static HandData ConvertToHandData(Hand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "Hand object cannot be null.");
            }

            // 초기화된 HandData 구조체 생성
            HandData handData = new HandData
            {
                ClosedTiles = new int[35],
                OpenedTiles = new int[35],
                CallBlocks = new BlockData[14],
                WinningTile = hand.WinningTile,
                CallBlockCount = 0
            };

            // ClosedTiles 리스트를 배열로 복사 (길이 35 보장)
            for (int i = 0; i < hand.ClosedTiles.Count && i < 35; i++)
            {
                handData.ClosedTiles[i] = hand.ClosedTiles[i];
            }

            // OpenedTiles 리스트를 배열로 복사 (길이 35 보장)
            for (int i = 0; i < hand.OpenedTiles.Count && i < 35; i++)
            {
                handData.OpenedTiles[i] = hand.OpenedTiles[i];
            }

            // CallBlocks 리스트를 BlockData 배열로 변환 (최대 14개)
            for (int i = 0; i < hand.CallBlocks.Count && i < 14; i++)
            {
                Block block = hand.CallBlocks[i];
                handData.CallBlocks[i] = new BlockData
                {
                    Type = (int)block.Type,
                    Tile = block.Tile,
                    Source = (int)block.Source,
                    SourceTileIndex = block.SourceTileIndex
                };
                handData.CallBlockCount++;
            }

            return handData;
        }

        public static WinningConditionData ConvertToWinningConditionData(WinningCondition condition, int roundWind, int seatWind)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition), "WinningCondition object cannot be null.");
            }

            return new WinningConditionData
            {
                WinningTile = condition.WinningTile,
                IsDiscarded = condition.IsDiscarded ? 1 : 0,
                IsLastTileInTheGame = condition.IsLastTileInTheGame ? 1 : 0,
                IsLastTileOfItsKind = condition.IsLastTileOfItsKind ? 1 : 0,
                IsReplacementTile = condition.IsReplacementTile ? 1 : 0,
                IsRobbingTheKong = condition.IsRobbingTheKong ? 1 : 0,
                RoundWind = roundWind,
                SeatWind = seatWind
            };
        }
    }

    public static class ScoreCalculatorInterop
    {
        private const string DllName = "ScoreCalculator";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern TileScoreDataArray GetTenpaiTileScoreData(HandData hand, WinningConditionData condition);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern YakuScoreDataArray GetHuYakuScoreData(HandData hand, WinningConditionData condition);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeTileScoreData(IntPtr data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeYakuScoreData(IntPtr data);

        public static List<TileScoreData> GetTenpaiTileScores(HandData handData, WinningConditionData conditionData)
        {
            // DLL 함수 호출
            TileScoreDataArray resultArray = GetTenpaiTileScoreData(handData, conditionData);

            if (resultArray.Count <= 0 || resultArray.Data == IntPtr.Zero)
            {
                return new List<TileScoreData>();
            }

            // 결과를 배열로 변환
            TileScoreData[] tileScores = new TileScoreData[resultArray.Count];

            IntPtr currentPtr = resultArray.Data;

            for (int i = 0; i < resultArray.Count; i++)
            {
                tileScores[i] = Marshal.PtrToStructure<TileScoreData>(currentPtr);
                currentPtr = IntPtr.Add(currentPtr, Marshal.SizeOf(typeof(TileScoreData)));
            }

            // 메모리 해제
            FreeTileScoreData(resultArray.Data);

            return new List<TileScoreData>(tileScores);
        }


        public static List<YakuScoreData> GetHuYakuScores(HandData handData, WinningConditionData conditionData)
        {
            // DLL 함수 호출
            YakuScoreDataArray resultArray = GetHuYakuScoreData(handData, conditionData);

            if (resultArray.Count <= 0 || resultArray.Data == IntPtr.Zero)
            {
                return new List<YakuScoreData>();
            }

            // 결과를 배열로 변환
            YakuScoreData[] yakuScores = new YakuScoreData[resultArray.Count];

            IntPtr currentPtr = resultArray.Data;

            for (int i = 0; i < resultArray.Count; i++)
            {
                yakuScores[i] = Marshal.PtrToStructure<YakuScoreData>(currentPtr);
                currentPtr = IntPtr.Add(currentPtr, Marshal.SizeOf(typeof(YakuScoreData)));
            }

            // 메모리 해제
            FreeYakuScoreData(resultArray.Data);

            return new List<YakuScoreData>(yakuScores);
        }
    }

    // 변환 유틸리티
    public static class Converter
    {
        // Block → BlockData 변환
        public static BlockData ConvertToBlockData(Block block)
        {
            return new BlockData
            {
                Type = (int)block.Type,
                Tile = block.Tile,
                Source = (int)block.Source,
                SourceTileIndex = block.SourceTileIndex
            };
        }

        // WinningCondition → WinningConditionData 변환
        public static WinningConditionData ConvertToWinningConditionData(WinningCondition condition)
        {
            return new WinningConditionData
            {
                WinningTile = condition.WinningTile,
                IsDiscarded = condition.IsDiscarded ? 1 : 0,
                IsLastTileInTheGame = condition.IsLastTileInTheGame ? 1 : 0,
                IsLastTileOfItsKind = condition.IsLastTileOfItsKind ? 1 : 0,
                IsReplacementTile = condition.IsReplacementTile ? 1 : 0,
                IsRobbingTheKong = condition.IsRobbingTheKong ? 1 : 0
            };
        }

        // Hand → HandData 변환
        public static HandData ConvertToHandData(Hand hand)
        {
            const int maxCallBlocks = 14;

            return new HandData
            {
                ClosedTiles = hand.ClosedTiles.ToArray(),
                OpenedTiles = hand.OpenedTiles.ToArray(),
                WinningTile = hand.WinningTile,
                CallBlocks = hand.CallBlocks
                    .Take(maxCallBlocks)
                    .Select(ConvertToBlockData)
                    .ToArray(),
                CallBlockCount = Math.Min(hand.CallBlocks.Count, maxCallBlocks)
            };
        }
    }
}
