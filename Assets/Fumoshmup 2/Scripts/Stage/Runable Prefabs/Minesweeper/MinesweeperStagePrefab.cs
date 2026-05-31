using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FumoShmup2
{
    #region Utils
    internal static class MinesweeperExtensions
    {
        public static int Clamp(this int i, int min, int max)
        {
            return Mathf.Clamp(i, min, max);
        }
        public static string SpaceByCapitals(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            var result = new System.Text.StringBuilder();
            result.Append(input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }
            return result.ToString();
        }
        public static string ToSpacedString(this Enum key)
        {
            return key.ToString().SpaceByCapitals();
        }
        public static Color MinesweeperDigitColor(this int digit)
        {
            switch (digit)
            {
                case 1: return Color.blue;
                case 2: return Color.green;
                case 3: return Color.red;
                case 4: return new Color(0f, 0f, 0.5f);
                case 5: return new Color(0.5f, 0f, 0f);
                case 6: return new Color(0f, 0.5f, 0.5f);
                case 7: return Color.black;
                case 8: return Color.gray;
                default: return Color.clear;
            }
        }
    }
    public partial class MinesweeperStagePrefab
    {
        public static class MinesweeperUtils
        {
            public static bool IsBomb(MinesweeperTile s)
            {
                return (s.state & (MinesweeperTile.State.BombBrick | MinesweeperTile.State.BombVisible | MinesweeperTile.State.BombTriggered | MinesweeperTile.State.CorrectFlag)) != 0;
            }
            public static bool TryGetRandomTile(Dictionary<(int, int), MinesweeperTile> board, int sizeX, int sizeY, out MinesweeperTile result)
            {
                result = null;
                if (board == null || board.Count == 0)
                    return false;

                int x = UnityEngine.Random.Range(0, sizeX);
                int y = UnityEngine.Random.Range(0, sizeY);
                return board.TryGetValue((x, y), out result);
            }
        }
    }
    #endregion
    #region Click Tile Actions
    public partial class MinesweeperStagePrefab
    {
        public static void SendLeftClick((int, int) tile)
        {
            if (instance != null && playBoard.TryGetValue(tile, out MinesweeperTile item))
            {
                instance.OnLeftClick(item);
            }
        }
        public static void SendRightClick((int, int) tile)
        {
            if (instance != null && playBoard.TryGetValue(tile, out MinesweeperTile item))
            {
                instance.OnRightClick(item);
            }
        }
        private void OnLeftClick(MinesweeperTile tile)
        {
            if (!GameStarted)
            {
                GameStarted = true;
                IsPlaying = true;
            }
            if (!IsPlaying)
            {
                return;
            }

            if (MovesCount == 0)
            {
                //TryMoveBombFromTileToRandomTile(tile, 1);
                ForceSafeArea(tile, 9);
            }

            if (tile.state.HasFlag(MinesweeperTile.State.CorrectFlag) || tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
                return;

            if (tile.state.HasFlag(MinesweeperTile.State.BombBrick))
            {
                MovesCount++;
                tile.UpdateState(MinesweeperTile.State.BombTriggered);
                RevealEntireBoardWithBomb(tile);
                IsPlaying = false;
                return;
            }

            if (tile.state.HasFlag(MinesweeperTile.State.Brick))
            {
                tile.UpdateState(MinesweeperTile.State.Cleared);
                MovesCount++;

                if (tile.NearbyBombs == 0)
                {
                    RevealNeighbors(tile);
                }
                else if (MovesCount == 1)
                {
                    RevealFirstEmptyNeighbor(tile);
                }
                if (RemainingClickableTiles <= 0)
                {
                    RevealEntireBoardWin();
                }
            }
        }
        private void OnRightClick(MinesweeperTile tile)
        {
            if (!IsPlaying || !GameStarted)
                return;
            if (tile.state.HasFlag(MinesweeperTile.State.Cleared))
                return;

            if (tile.state.HasFlag(MinesweeperTile.State.CorrectFlag))
            {
                tile.UpdateState(MinesweeperTile.State.BombBrick);
                return;
            }

            if (tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
            {
                tile.UpdateState(MinesweeperTile.State.Brick);
                return;
            }

            if (tile.IsBomb)
            {
                tile.UpdateState(MinesweeperTile.State.CorrectFlag);
            }
            else
            {
                tile.UpdateState(MinesweeperTile.State.FalseFlag);
            }
        }
        private void RevealNeighbors(MinesweeperTile tile)
        {
            int x = tile.tileXY.Item1;
            int y = tile.tileXY.Item2;

            for (int i = (x - 1).Clamp(0, BoardSize.Item1 - 1); i <= (x + 1).Clamp(0, BoardSize.Item1 - 1); i++)
            {
                for (int j = (y - 1).Clamp(0, BoardSize.Item2 - 1); j <= (y + 1).Clamp(0, BoardSize.Item2 - 1); j++)
                {
                    if ((i, j) == tile.tileXY)
                        continue;

                    if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor))
                    {
                        if (neighbor.state.HasFlag(MinesweeperTile.State.Brick) &&
                            !neighbor.state.HasFlag(MinesweeperTile.State.Cleared))
                        {
                            neighbor.UpdateState(MinesweeperTile.State.Cleared);
                            if (neighbor.NearbyBombs == 0)
                                RevealNeighbors(neighbor);
                        }
                    }
                }
            }
        }
        private void ForceSafeArea(MinesweeperTile startTile, int desiredTileClears)
        {
            HashSet<(int, int)> safeRegion = new();
            Queue<MinesweeperTile> frontier = new();

            frontier.Enqueue(startTile);
            safeRegion.Add(startTile.tileXY);

            while (frontier.Count > 0 &&
                   safeRegion.Count < desiredTileClears)
            {
                var current = frontier.Dequeue();

                int x = current.tileXY.Item1;
                int y = current.tileXY.Item2;

                for (int i = (x - 1).Clamp(0, BoardSize.Item1 - 1); i <= (x + 1).Clamp(0, BoardSize.Item1 - 1); i++)
                {
                    for (int j = (y - 1).Clamp(0, BoardSize.Item2 - 1); j <= (y + 1).Clamp(0, BoardSize.Item2 - 1); j++)
                    {
                        if (!playBoard.TryGetValue((i, j), out var neighbor))
                            continue;

                        if (safeRegion.Add(neighbor.tileXY))
                        {
                            frontier.Enqueue(neighbor);

                            if (safeRegion.Count >= desiredTileClears)
                                break;
                        }
                    }
                }
            }

            List<MinesweeperTile> bombsToMove = new();

            foreach (var pos in safeRegion)
            {
                if (playBoard.TryGetValue(pos, out var tile) && tile.IsBomb)
                {
                    bombsToMove.Add(tile);
                }
            }

            foreach (var bomb in bombsToMove)
            {
                bomb.UpdateState(MinesweeperTile.State.Brick);
            }

            foreach (var bomb in bombsToMove)
            {
                while (MinesweeperUtils.TryGetRandomTile(
                    playBoard,
                    BoardSize.Item1,
                    BoardSize.Item2,
                    out var target))
                {
                    if (target.IsBomb)
                        continue;

                    if (safeRegion.Contains(target.tileXY))
                        continue;

                    target.PlaceBomb();
                    break;
                }
            }
        }
        private void RevealFirstEmptyNeighbor(MinesweeperTile tile)
        {
            int x = tile.tileXY.Item1;
            int y = tile.tileXY.Item2;

            for (int i = (x - 1).Clamp(0, BoardSize.Item1 - 1); i <= (x + 1).Clamp(0, BoardSize.Item1 - 1); i++)
            {
                for (int j = (y - 1).Clamp(0, BoardSize.Item2 - 1); j <= (y + 1).Clamp(0, BoardSize.Item2 - 1); j++)
                {
                    if ((i, j) == tile.tileXY) continue;

                    if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor))
                    {
                        if (neighbor.state.HasFlag(MinesweeperTile.State.Brick) && neighbor.NearbyBombs == 0)
                        {
                            neighbor.UpdateState(MinesweeperTile.State.Cleared);
                            RevealNeighbors(neighbor);
                            return;
                        }
                    }
                }
            }
        }
        private void RevealEntireBoardWin()
        {
            if (!IsPlaying)
                return;

            foreach (var kvp in playBoard)
            {
                var tile = kvp.Value;
                if (tile.IsBomb)
                {
                    tile.UpdateState(MinesweeperTile.State.BombVisible);
                }
                else if (tile.state.HasFlag(MinesweeperTile.State.Brick))
                {
                    tile.UpdateState(MinesweeperTile.State.Cleared);
                }
            }
            IsPlaying = false;
            GameWin = true;
        }
        private void RevealEntireBoardWithBomb(MinesweeperTile bombedTile)
        {
            GameLost = true;
            foreach (var kvp in playBoard)
            {
                var tile = kvp.Value;
                if (tile.state == MinesweeperTile.State.FalseFlag)
                {
                    tile.UpdateState(MinesweeperTile.State.Brick);
                }

                if (tile.IsBomb)
                {
                    tile.UpdateState(MinesweeperTile.State.BombVisible);
                }
                else if (tile.state.HasFlag(MinesweeperTile.State.Brick))
                {
                    tile.UpdateState(MinesweeperTile.State.Cleared);
                }
                bombedTile.UpdateState(MinesweeperTile.State.BombTriggered);
            }
        }

        private void TryMoveBombFromTileToRandomTile(MinesweeperTile startTile, int tilesToClear = 8)
        {
            if (startTile == null) return;

            HashSet<(int, int)> visited = new();
            Queue<MinesweeperTile> frontier = new();

            frontier.Enqueue(startTile);
            visited.Add(startTile.tileXY);

            while (frontier.Count > 0 && visited.Count < tilesToClear)
            {
                MinesweeperTile current = frontier.Dequeue();
                int x = current.tileXY.Item1;
                int y = current.tileXY.Item2;

                List<MinesweeperTile> neighbors = new();

                for (int i = (x - 1).Clamp(0, BoardSize.Item1 - 1); i <= (x + 1).Clamp(0, BoardSize.Item1 - 1); i++)
                {
                    for (int j = (y - 1).Clamp(0, BoardSize.Item2 - 1); j <= (y + 1).Clamp(0, BoardSize.Item2 - 1); j++)
                    {
                        if ((i, j) == current.tileXY) continue;

                        if (playBoard.TryGetValue((i, j), out MinesweeperTile neighbor) &&
                            !visited.Contains(neighbor.tileXY))
                        {
                            neighbors.Add(neighbor);
                        }
                    }
                }

                for (int i = 0; i < neighbors.Count; i++)
                {
                    int swapIndex = UnityEngine.Random.Range(i, neighbors.Count);
                    MinesweeperTile temp = neighbors[i];
                    neighbors[i] = neighbors[swapIndex];
                    neighbors[swapIndex] = temp;
                }

                int expansionCount = Mathf.Min(UnityEngine.Random.Range(1, 4), neighbors.Count);

                for (int i = 0; i < expansionCount; i++)
                {
                    MinesweeperTile neighbor = neighbors[i];

                    if (visited.Add(neighbor.tileXY))
                        frontier.Enqueue(neighbor);
                }
            }

            List<MinesweeperTile> bombsToMove = new();

            foreach ((int, int) pos in visited)
            {
                if (playBoard.TryGetValue(pos, out MinesweeperTile tile) && tile.IsBomb)
                    bombsToMove.Add(tile);
            }

            foreach (MinesweeperTile bombTile in bombsToMove)
                bombTile.UpdateState(MinesweeperTile.State.Brick);

            foreach (MinesweeperTile bombTile in bombsToMove)
            {
                int attempts = 50000;

                while (attempts > 0)
                {
                    attempts--;

                    if (!MinesweeperUtils.TryGetRandomTile(playBoard, BoardSize.Item1, BoardSize.Item2, out MinesweeperTile randomTile))
                        continue;

                    if (randomTile.IsBomb)
                        continue;

                    if (visited.Contains(randomTile.tileXY))
                        continue;

                    randomTile.PlaceBomb();
                    break;
                }

                if (attempts <= 0)
                    Debug.LogWarning("Failed to relocate bomb.");
            }

            foreach ((int, int) pos in visited)
            {
                if (playBoard.TryGetValue(pos, out MinesweeperTile tile))
                {
                    if (!tile.IsBomb && tile.state.HasFlag(MinesweeperTile.State.Brick))
                    {
                        tile.UpdateState(MinesweeperTile.State.Cleared);

                        if (tile.NearbyBombs == 0)
                            RevealNeighbors(tile);
                    }
                }
            }
        }

    }
    #endregion
    public partial class MinesweeperStagePrefab
    {
        #region Board Building Methods
        private void BuildBoard(int quadraticSize, int bombsToPlace, out WaitUntil wait)
        {
            if (playBoard == null)
            {
                playBoard = new();
            }
            DestroyBoardItems();
            MovesCount = 0;
            BoardSize = new(quadraticSize, quadraticSize);
            bombsToPlace = bombsToPlace.Clamp(1, ((int)((quadraticSize * quadraticSize) * 0.8f)));
            for (int i = 0; i < quadraticSize; i++)
            {
                for (int j = 0; j < quadraticSize; j++)
                {
                    Vector2 center = new Vector2Shmup(0.5f, 0.55f).Vector2Now;
                    float size = TileSize.x;
                    Vector2 pos = center + new Vector2(-4f + size * i, -4f + size * j);
                    MinesweeperTile tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                    tile.transform.SetParent(transform);
                    tile.UpdateState(MinesweeperTile.State.Brick);
                    tile.gameObject.SetActive(true);
                    tile.tileXY = new(i, j);
                    playBoard.Add((i, j), tile);
                }
            }
            int attempts = 50000;
            while (attempts > 0 && BombCount < bombsToPlace && MinesweeperUtils.TryGetRandomTile(playBoard, quadraticSize, quadraticSize, out MinesweeperTile result))
            {
                attempts--;
                if (!result.state.HasFlag(MinesweeperTile.State.BombBrick))
                {
                    result.PlaceBomb();
                }
            }
            if (attempts <= 0 && BombCount < bombsToPlace)
            {
                Debug.LogError("Failed to create board fully in 50000 moves.");
            }
            GameStarted = false;
            IsPlaying = true;
            GameWin = false;
            GameLost = false;
            wait = new WaitUntil(() => GameWin || GameLost);
        }
        #endregion
        public static int BombCount
        {
            get
            {
                if (playBoard == null || playBoard.Count == 0)
                {
                    return 0;
                }
                int bombCount = 0;
                foreach (var kvp in playBoard)
                {
                    if (kvp.Value.IsBomb)
                    {
                        bombCount += 1;
                    }
                }
                return bombCount;
            }
        }
        public static int RemainingClickableTiles
        {
            get
            {
                if (playBoard == null || playBoard.Count == 0)
                    return 0;

                int count = 0;
                foreach (var kvp in playBoard)
                {
                    var tile = kvp.Value;
                    if (tile.state.HasFlag(MinesweeperTile.State.Brick) ||
                        tile.state.HasFlag(MinesweeperTile.State.FalseFlag))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public static Dictionary<(int, int), MinesweeperTile> playBoard = new();
        public Vector2 TileSize
        {
            get
            {
                Vector2 a = new Vector2Shmup(0.09f, 0.09f).Vector2Now;
                Vector2 b = new Vector2Shmup(0f, 0f).Vector2Now;
                float size = (a.x - b.x).Absolute();
                return new Vector2(size, size);
            }
        }
        public static (int, int) BoardSize { get; private set; } = new(10, 10);
    }
    public partial class MinesweeperStagePrefab : FumoShmup2.StageRunablePrefab
    {
        public static bool GameStarted;
        public static bool GameWin;
        public static bool GameLost;
        public static bool IsPlaying;
        public static int MovesCount;

        static MinesweeperStagePrefab instance;

        [SerializeField] DialogueStackSO minesweeperStartDialogue;
        [SerializeField] MinesweeperTile tilePrefab;
        [SerializeField] MusicWrapper optionalMusic;
        [SerializeField] int MinesCount = 10;
        protected override IEnumerator RunablePayload()
        {
            if (minesweeperStartDialogue != null)
            {
                minesweeperStartDialogue.StartDialogue(out WaitUntil wait, null);
                yield return wait;
            }
            optionalMusic.Play();
            BuildBoard(10, MinesCount, out WaitUntil w);
            if (w == null)
            {
                yield break;
            }
            yield return w;
            yield return 2.5f.WaitForSeconds();
            Cleanup();
        }
        private void Cleanup()
        {
            DestroyBoardItems();
            Destroy(gameObject);
        }
        private void DestroyBoardItems()
        {
            playBoard.Clear();
        }
        private void Awake()
        {
            tilePrefab.gameObject.SetActive(false);
            instance = this;
        }
    }
}
