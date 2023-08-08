using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Web.Contrast.Pages
{
    public struct Masu
    {
        public int Koma { get; set; }
        public int Tile { get; set; }

        public Masu()
        {
        }
    }

    public partial class Index
    {
        public const int BoardSize = 5;
        public const int PlayerCount = 2;

        private Masu[,] _board = new Masu[BoardSize, BoardSize];
        private List<int> _havingTiles = new() { 2, 2, 3, -2, -2, -3 };

        private (int x, int y) _selectedKoma = (-1, -1);
        private int _selectedTile = 0;

        private Mode _mode = Mode.KomaSelecting;
        private Turn _currentTurn = Turn.Positive;


        enum Mode
        {
            Initialized,
            KomaSelecting,
            KomaMove,
            TileSelecting,
            TileMove,
        }

        enum Turn
        {
            Positive,
            Negative,
        }

        [Flags]
        enum KomaType
        {
            None = 0,
            Along = 0x01,
            Diagonal = 0x02,
        }

        protected override void OnInitialized()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            _board = new Masu[BoardSize, BoardSize];
            for (int i = 0; i < BoardSize; i++)
            {
                _board[0, i].Koma = -1;
                _board[BoardSize - 1, i].Koma = 1;
            }
            _havingTiles = new() { 2, 2, 3, -2, -2, -3 };
            _mode = Mode.Initialized;
        }

        private void OnPromptClick()
        {
            if (_mode == Mode.Initialized)
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            if (_mode != Mode.Initialized)
            {
                return;
            }
            _currentTurn = (Turn)Random.Shared.Next(0, 1);
            _mode = Mode.KomaSelecting;
        }

        private void OnMasuClick(MouseEventArgs e, int x, int y)
        {
            switch (_mode)
            {
                case Mode.KomaSelecting:
                case Mode.KomaMove:
                    if (_currentTurn == Turn.Positive && _board[x, y].Koma > 0
                        || _currentTurn == Turn.Negative && _board[x, y].Koma < 0)
                    {
                        _selectedKoma = (x, y);
                        _mode = Mode.KomaMove;
                        break;
                    }
                    if (_mode == Mode.KomaMove && CanMove(_selectedKoma, (x, y)))
                    {
                        _board[x, y].Koma = _board[_selectedKoma.x, _selectedKoma.y].Koma;
                        _board[_selectedKoma.x, _selectedKoma.y].Koma = 0;
                        _mode = Mode.TileSelecting;
                        break;
                    }
                    break;

                case Mode.TileSelecting:
                    break;

                case Mode.TileMove:
                    if (CanPlaceTile(_board[x, y]))
                    {
                        _board[x, y].Tile = _selectedTile;
                        _havingTiles.Remove(_selectedTile);
                        _selectedTile = 0;

                        GoNextTurn();
                    }
                    break;

                default:
                    break;
            }
        }

        private void OnClickTile(int tile)
        {
            if (_currentTurn == Turn.Negative && tile > 0)
            {
                return;
            }
            if (_currentTurn == Turn.Positive && tile < 0)
            {
                return;
            }
            if (_mode == Mode.TileSelecting || _mode == Mode.TileMove)
            {
                _selectedTile = tile;
                _mode = Mode.TileMove;
            }
        }

        private void GoNextTurn()
        {
            if (_mode == Mode.TileSelecting || _mode == Mode.TileMove)
            {
                _currentTurn = (Turn)((int)(_currentTurn + 1) % 2);
                _mode = Mode.KomaSelecting;
            }
        }

        private bool CanMove((int x, int y) source, (int x, int y) destination)
        {
            if (source == destination)
            {
                return false;
            }

            if (_board[destination.x, destination.y].Koma != 0)
            {
                return false;
            }

            KomaType ways = GetKomaType(_board[source.x, source.y]);
            if ((ways.HasFlag(KomaType.Along) && (source.x == destination.x || source.y == destination.y))
                || (ways.HasFlag(KomaType.Diagonal) && Math.Abs(source.x - destination.x) == Math.Abs(source.y - destination.y)))
            {
                if (CanMoveKomaCore(source, destination))
                {
                    return true;
                }
            }
            return false;
        }


        private bool CanMoveKomaCore((int x, int y) source, (int x, int y) destination)
        {
            int koma = _board[source.x, source.y].Koma;

            int dpX = 0;
            int dpY = 0;

            if (source.x > destination.x)
            {
                dpX = -1;
            }
            else if (source.x < destination.x)
            {
                dpX = +1;
            }

            if (source.y > destination.y)
            {
                dpY = -1;
            }
            else if (source.y < destination.y)
            {
                dpY = +1;
            }

            int x = source.x + dpX;
            int y = source.y + dpY;
            while ((x, y) != destination)
            {
                if (_board[x, y].Koma != koma)
                {
                    return false;
                }
                x += dpX;
                y += dpY;
            }
            return true;
        }

        private KomaType GetKomaType(Masu masu)
        {
            if (masu.Koma == 0)
            {
                return KomaType.None;
            }

            return Math.Abs(masu.Tile) switch
            {
                2 => KomaType.Diagonal,
                3 => KomaType.Along | KomaType.Diagonal,
                _ => KomaType.Along
            };
        }

        private MarkupString DrawKoma(int x, int y)
        {
            if (x < 0 || x >= _board.GetLength(0))
            {
                return (MarkupString)"";
            }
            if (y < 0 || y >= _board.GetLength(1))
            {
                return (MarkupString)"";
            }

            int koma = _board[x, y].Koma;
            if (koma == 0)
            {
                return (MarkupString)"";
            }

            string name = koma > 0 ? "koma1" : "koma2";

            return (MarkupString)(Math.Abs(_board[x, y].Tile) switch
            {
                2 => $"<img src='/resources/koma_black.png' class='koma {name}'/>",
                3 => $"<img src='/resources/koma_gray.png'  class='koma {name}'/>",
                _ => $"<img src='/resources/koma_white.png' class='koma {name}'/>",
            });
        }

        private MarkupString GetTileClasses(int x, int y)
        {
            if (x < 0 || x >= _board.GetLength(0))
            {
                return (MarkupString)"";
            }
            if (y < 0 || y >= _board.GetLength(1))
            {
                return (MarkupString)"";
            }

            int tile = _board[x, y].Tile;

            return GetTileClasses(tile);
        }

        private static MarkupString GetTileClasses(int tile)
        {
            return (MarkupString)(Math.Abs(tile) switch
            {
                2 => $"black",
                3 => $"gray",
                _ => $"",
            });
        }

        private MarkupString GetMasuHighlight(int x, int y)
        {
            if (_mode == Mode.KomaMove && CanMove(_selectedKoma, (x, y)))
            {
                return (MarkupString)"can-move";
            }
            if (_mode == Mode.TileMove && CanPlaceTile(_board[x, y]))
            {
                return (MarkupString)"can-move";
            }
            return (MarkupString)"";
        }

        private static bool CanPlaceTile(Masu masu)
        {
            return masu switch
            {
                { Koma: 0, Tile: 0 } => true,
                _ => false,
            };
        }

        private void ForEachMasu(Action<int, int> action)
        {
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    action(i, j);
                }
            }
        }
    }
}
