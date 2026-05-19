using rinCore;
using System;
using TMPro;
using UnityEngine;

namespace FumoShmup2
{
    public class MinesweeperTile : MonoBehaviour
    {
        [SerializeField] BoxCollider2D _collider;
        [SerializeField] SpriteRenderer _tileSprite;
        [SerializeField] TMP_Text _text;
        [Flags]
        public enum State
        {
            None = 0,
            Cleared = 1 << 0,
            CorrectFlag = 1 << 1,
            FalseFlag = 1 << 2,
            BombBrick = 1 << 3,
            BombTriggered = 1 << 4,
            BombVisible = 1 << 5,
            Brick = 1 << 6
        }
        public State state { get; private set; } = State.None;
        public MinesweeperTile(State s)
        {
            state = s;
        }
        public (int, int) tileXY;
        public bool IsBomb => MinesweeperStagePrefab.MinesweeperUtils.IsBomb(this);
        public MinesweeperTile PlaceBomb()
        {
            return UpdateState(State.BombBrick);
        }
        private void Update()
        {
            bool leftClick = ShmupInput.ShootJustPressed;
            bool rightClick = ShmupInput.FocusJustPressed;
            if (leftClick || rightClick)
            {
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p) && p.IsAlive)
                {
                    if (_collider.bounds.Contains(p.CurrentPosition))
                    {
                        (int, int) xy = this.tileXY;
                        if (leftClick) MinesweeperStagePrefab.SendLeftClick(xy);
                        if (rightClick) MinesweeperStagePrefab.SendRightClick(xy);
                    }
                }
            }
        }
        public MinesweeperTile UpdateState(State s)
        {
            state = s;
            _text.text = "";
            switch (state)
            {
                case State.None:
                    _tileSprite.color = ColorHelper.Gray7;
                    break;
                case State.Cleared:
                    int NearbyBombs = this.NearbyBombs;
                    _text.color = MinesweeperExtensions.MinesweeperDigitColor(NearbyBombs);
                    _text.text = NearbyBombs.ToString();
                    _tileSprite.color = ColorHelper.Gray4;
                    if (!MinesweeperStagePrefab.GameLost)
                        PointItemRunner.SpawnPointItem(transform.position);
                    break;
                case State.CorrectFlag:
                    _tileSprite.color = ColorHelper.PastelYellow;
                    break;
                case State.FalseFlag:
                    _tileSprite.color = ColorHelper.PastelYellow;
                    break;
                case State.BombBrick:
                    _tileSprite.color = ColorHelper.Gray7;
                    break;
                case State.BombTriggered:
                    GeneralManager.FunnyExplosion(new() { is3d = false, position = transform.position, playSound = true, scale = 3f });
                    if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                    {
                        p.SendHit(new(transform.position, new(null, 1f, 1f)), out _);
                    }
                    _tileSprite.color = ColorHelper.PastelRed;
                    break;
                case State.BombVisible:
                    _tileSprite.color = ColorHelper.DeepRed;
                    break;
                case State.Brick:
                    _tileSprite.color = ColorHelper.Gray7;
                    break;
                default:
                    break;
            }
            return this;
        }
        public int NearbyBombs
        {
            get
            {
                int count = 0;
                int x = tileXY.Item1;
                int y = tileXY.Item2;

                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (i == x && j == y) continue;
                        if (i < 0 || j < 0 || i >= MinesweeperStagePrefab.BoardSize.Item1 || j >= MinesweeperStagePrefab.BoardSize.Item2) continue;

                        if (MinesweeperStagePrefab.playBoard.TryGetValue((i, j), out MinesweeperTile neighbor) && neighbor.IsBomb)
                            count++;
                    }
                }

                return count;
            }
        }
    }
}
