using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SnakeWPF
{
    public partial class MainWindow : Window
    {
        private Random rnd = new Random();
        private System.Windows.Threading.DispatcherTimer gameTickTimer = new System.Windows.Threading.DispatcherTimer();

        private int currentScore = 0;

        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;

        private SolidColorBrush snakeBodyBrush = Brushes.Cyan;
        private SolidColorBrush snakeHeadBrush = Brushes.DarkCyan;
        private List<SnakePart> snakeParts = new List<SnakePart>();

        private enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        private int snakeLength;

        private UIElement snakeFood = null;
        private SolidColorBrush foodBrush = Brushes.OrangeRed;

        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
        }

        private void GameTickTimer_Tick(object sender, EventArgs e) {
            MoveSnake();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            StartNewGame();
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch(e.Key) {
                case System.Windows.Input.Key.Up:
                    if(snakeDirection != SnakeDirection.Down) {
                        snakeDirection = SnakeDirection.Up;
                    }
                    break;
                case System.Windows.Input.Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                    {
                        snakeDirection = SnakeDirection.Down;
                    }
                    break;
                case System.Windows.Input.Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                    {
                        snakeDirection = SnakeDirection.Right;
                    }
                    break;
                case System.Windows.Input.Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                    {
                        snakeDirection = SnakeDirection.Left;
                    }
                    break;
                case System.Windows.Input.Key.Escape:
                    StartNewGame();
                    break;
            }
            if(originalSnakeDirection != snakeDirection) {
                MoveSnake();
            }
        }

        private void DrawGameArea() {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while(doneDrawingBackground == false) {
                Rectangle rectangle = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.LightCyan
                };
                GameArea.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;

                if(nextX >= GameArea.ActualWidth) {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if(nextY >= GameArea.ActualHeight) {
                    doneDrawingBackground = true;
                }
            }
        }

        private void StartNewGame() {
            foreach (SnakePart snakeBodyPart in snakeParts) {
                if (snakeBodyPart.UiElement != null) {
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
                }
            }
            snakeParts.Clear();

            if (snakeFood != null) {
                GameArea.Children.Remove(snakeFood);
            }

            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart() {
                Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5)
            });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            DrawSnake();
            DrawSnakeFood();
            UpdateGameStatus();
            gameTickTimer.IsEnabled = true;
        }

        private void MoveSnake() {
            while(snakeParts.Count >= snakeLength) {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            foreach(SnakePart snakePart in snakeParts) {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch(snakeDirection) {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            snakeParts.Add(new SnakePart() {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            DrawSnake();
            DoCollisionCheck();
        }

        private void DrawSnake() {
            foreach(SnakePart snakePart in snakeParts) {
                if(snakePart.UiElement == null) {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        private void DrawSnakeFood() {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse() {
                Width = SnakeSquareSize,
                Height= SnakeSquareSize,
                Fill = foodBrush
            };
            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.X);
            Canvas.SetLeft(snakeFood, foodPosition.Y);
        }

        private Point GetNextFoodPosition() {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

            foreach(SnakePart snakePart in snakeParts) {
                if(snakePart.Position.X == foodX && snakePart.Position.Y == foodY) {
                    return GetNextFoodPosition();
                }
            }

            return new Point(foodX, foodY);
        }

        private void DoCollisionCheck() {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            if(snakeHead.Position.X == Canvas.GetLeft(snakeFood) && snakeHead.Position.Y == Canvas.GetTop(snakeFood)) {
                EatSnakeFood();
                return;
            }

            if(snakeHead.Position.Y < 0 || snakeHead.Position.Y >= GameArea.ActualHeight ||
                snakeHead.Position.X < 0 || snakeHead.Position.X >= GameArea.ActualWidth) {
                EndGame();
            }

            foreach(SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1)) {
                if(snakeHead.Position.X == snakeBodyPart.Position.X && snakeHead.Position.Y == snakeBodyPart.Position.Y) {
                    EndGame();
                }
            }
        }

        private void EatSnakeFood() {
            snakeLength++;
            currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            this.Title = "SnakeWPF - Score: " + currentScore + " - Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
        }
        private void EndGame()
        {
            gameTickTimer.IsEnabled = false;
            MessageBox.Show("You died! Better luck next time!\n\nTo start a new game, press Esc.", "SnakeWPF");
        }
    }
}
