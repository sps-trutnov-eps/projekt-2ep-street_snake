using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreetSnake
{
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D squareTexture;
        private SpriteFont gameFont;
        private GameState currentGameState;
        private Button startButton;
        private Button exitButton;

        private const int GRID_SIZE = 20;
        private const int GRID_WIDTH = 40;
        private const int GRID_HEIGHT = 30;
        private const float INITIAL_MOVE_INTERVAL = 0.3f;
        private const float OBSTACLE_SPAWN_INTERVAL = 5f;
        private const float OBSTACLE_LIFETIME = 8f;

        private List<Vector2> snakeBody;
        private Vector2 direction;
        private Vector2 foodPosition;
        private Vector2 powerUpPosition;
        private List<ObstacleInfo> obstacles;
        private PowerUpType currentPowerUp;
        private float moveTimer;
        private float obstacleTimer;
        private float currentMoveInterval;
        private float powerUpTimer;
        private bool hasShield;
        private bool doublePoints;
        private int score;
        private bool isGameOver;
        private Random random;

        private class ObstacleInfo
        {
            public Vector2 Position { get; set; }
            public float TimeRemaining { get; set; }
            public Color Color { get; set; }

            public ObstacleInfo(Vector2 position, float lifetime)
            {
                Position = position;
                TimeRemaining = lifetime;
                Color = Color.Gray;
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = GRID_WIDTH * GRID_SIZE;
            graphics.PreferredBackBufferHeight = GRID_HEIGHT * GRID_SIZE;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            random = new Random();
            currentGameState = GameState.MainMenu;
            startButton = new Button("Start", new Vector2(GRID_WIDTH / 2f * GRID_SIZE - 50, GRID_HEIGHT / 2f * GRID_SIZE - 50), StartGame);
            exitButton = new Button("Exit", new Vector2(GRID_WIDTH / 2f * GRID_SIZE - 50, GRID_HEIGHT / 2f * GRID_SIZE + 20), ExitGame);
            base.Initialize();
        }

        private void StartGame()
        {
            InitializeGame();
            currentGameState = GameState.Playing;
        }

        private void ExitGame()
        {
            Exit();
        }

        private void InitializeGame()
        {
            snakeBody = new List<Vector2>
            {
                new Vector2(GRID_WIDTH / 2, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 2 - 1, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 2 - 2, GRID_HEIGHT / 2)
            };

            direction = Vector2.UnitX;
            currentMoveInterval = INITIAL_MOVE_INTERVAL;
            moveTimer = 0;
            obstacleTimer = 0;
            powerUpTimer = 0;
            score = 0;
            isGameOver = false;
            obstacles = new List<ObstacleInfo>();
            hasShield = false;
            doublePoints = false;

            PlaceFood();
            PlacePowerUp();
            SpawnObstacles();
        }

        private void SpawnObstacles()
        {
            int obstacleCount = random.Next(3, 7);
            for (int i = 0; i < obstacleCount; i++)
            {
                TryAddObstacle();
            }
        }

        private void TryAddObstacle()
        {
            Vector2 position;
            int maxAttempts = 50;
            int attempts = 0;

            do
            {
                position = new Vector2(
                    random.Next(0, GRID_WIDTH),
                    random.Next(0, GRID_HEIGHT)
                );
                attempts++;

                if (!snakeBody.Contains(position) &&
                    !obstacles.Any(o => o.Position == position) &&
                    position != foodPosition &&
                    position != powerUpPosition &&
                    Vector2.Distance(position, snakeBody[0]) > 3)
                {
                    obstacles.Add(new ObstacleInfo(position, OBSTACLE_LIFETIME));
                    return;
                }
            } while (attempts < maxAttempts);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            squareTexture = new Texture2D(GraphicsDevice, 1, 1);
            squareTexture.SetData(new[] { Color.White });
            gameFont = Content.Load<SpriteFont>("GameFont");

            startButton.LoadContent(gameFont);
            exitButton.LoadContent(gameFont);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (currentGameState == GameState.MainMenu)
            {
                startButton.Update();
                exitButton.Update();
            }
            else if (currentGameState == GameState.GameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.R))
                {
                    InitializeGame();
                    currentGameState = GameState.Playing;
                    isGameOver = false;
                }
            }
            else if (currentGameState == GameState.Playing)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                obstacleTimer += elapsed;
                if (obstacleTimer >= OBSTACLE_SPAWN_INTERVAL)
                {
                    obstacleTimer = 0;
                    TryAddObstacle();
                }

                UpdateObstacles(elapsed);
                HandleInput();
                UpdateGame(gameTime);
            }

            base.Update(gameTime);
        }

        private void UpdateObstacles(float elapsed)
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                obstacles[i].TimeRemaining -= elapsed;

                float fadeStart = 2f;
                if (obstacles[i].TimeRemaining <= fadeStart)
                {
                    float alpha = obstacles[i].TimeRemaining / fadeStart;
                    obstacles[i].Color = Color.Gray * alpha;
                }

                if (obstacles[i].TimeRemaining <= 0)
                {
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void HandleInput()
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Up) && direction != Vector2.UnitY)
                direction = -Vector2.UnitY;
            else if (keyboardState.IsKeyDown(Keys.Down) && direction != -Vector2.UnitY)
                direction = Vector2.UnitY;
            else if (keyboardState.IsKeyDown(Keys.Left) && direction != Vector2.UnitX)
                direction = -Vector2.UnitX;
            else if (keyboardState.IsKeyDown(Keys.Right) && direction != -Vector2.UnitX)
                direction = Vector2.UnitX;
        }

        private void UpdateGame(GameTime gameTime)
        {
            moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (hasShield || doublePoints || currentMoveInterval < INITIAL_MOVE_INTERVAL)
            {
                powerUpTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (powerUpTimer >= 10f)
                {
                    ResetPowerUps();
                }
            }

            if (moveTimer >= currentMoveInterval)
            {
                moveTimer = 0;
                MoveSnake();
                CheckCollisions();
            }
        }

        private void MoveSnake()
        {
            Vector2 newHead = snakeBody[0] + direction;
            snakeBody.Insert(0, newHead);
            snakeBody.RemoveAt(snakeBody.Count - 1);
        }

        private void CheckCollisions()
        {
            Vector2 head = snakeBody[0];

            if (head.X < 0 || head.X >= GRID_WIDTH || head.Y < 0 || head.Y >= GRID_HEIGHT ||
                obstacles.Any(o => o.Position == head))
            {
                if (hasShield)
                {
                    hasShield = false;
                    snakeBody.RemoveAt(snakeBody.Count - 1);
                    return;
                }
                isGameOver = true;
                currentGameState = GameState.GameOver;
                return;
            }

            if (snakeBody.Skip(1).Any(segment => segment == head))
            {
                if (hasShield)
                {
                    hasShield = false;
                    snakeBody.RemoveAt(snakeBody.Count - 1);
                    return;
                }
                isGameOver = true;
                currentGameState = GameState.GameOver;
                return;
            }

            if (head == foodPosition)
            {
                score += doublePoints ? 2 : 1;
                snakeBody.Add(snakeBody[snakeBody.Count - 1]);
                PlaceFood();
            }

            if (head == powerUpPosition)
            {
                ApplyPowerUp();
                PlacePowerUp();
            }
        }

        private void ApplyPowerUp()
        {
            powerUpTimer = 0;
            switch (currentPowerUp)
            {
                case PowerUpType.Speed:
                    currentMoveInterval = INITIAL_MOVE_INTERVAL / 2;
                    break;
                case PowerUpType.Shield:
                    hasShield = true;
                    break;
                case PowerUpType.DoublePoints:
                    doublePoints = true;
                    break;
            }
        }

        private void ResetPowerUps()
        {
            currentMoveInterval = INITIAL_MOVE_INTERVAL;
            hasShield = false;
            doublePoints = false;
            powerUpTimer = 0;
        }

        private void PlaceFood()
        {
            do
            {
                foodPosition = new Vector2(
                    random.Next(0, GRID_WIDTH),
                    random.Next(0, GRID_HEIGHT)
                );
            } while (snakeBody.Contains(foodPosition) ||
                     obstacles.Any(o => o.Position == foodPosition));
        }

        private void PlacePowerUp()
        {
            currentPowerUp = (PowerUpType)random.Next(0, 3);
            do
            {
                powerUpPosition = new Vector2(
                    random.Next(0, GRID_WIDTH),
                    random.Next(0, GRID_HEIGHT)
                );
            } while (snakeBody.Contains(powerUpPosition) ||
                     obstacles.Any(o => o.Position == powerUpPosition) ||
                     powerUpPosition == foodPosition);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            if (currentGameState == GameState.MainMenu)
            {
                startButton.Draw(spriteBatch);
                exitButton.Draw(spriteBatch);
            }
            else if (currentGameState == GameState.Playing)
            {
                foreach (var obstacle in obstacles)
                {
                    DrawSquare(obstacle.Position, obstacle.Color);
                }

                for (int i = 0; i < snakeBody.Count; i++)
                {
                    Color snakeColor = hasShield ? Color.Gold : Color.Green;
                    DrawSquare(snakeBody[i], i == 0 ? Color.LightGreen : snakeColor);
                }

                DrawSquare(foodPosition, Color.Red);

                Color powerUpColor = currentPowerUp switch
                {
                    PowerUpType.Speed => Color.Blue,
                    PowerUpType.Shield => Color.Yellow,
                    PowerUpType.DoublePoints => Color.Purple,
                    _ => Color.White
                };
                DrawSquare(powerUpPosition, powerUpColor);

                string statusText = $"Score: {score}";
                if (hasShield) statusText += " SHIELD";
                if (currentMoveInterval < INITIAL_MOVE_INTERVAL) statusText += " SPEED";
                if (doublePoints) statusText += " 2X";

                spriteBatch.DrawString(gameFont, statusText, new Vector2(5, 5), Color.White);
            }
            else if (currentGameState == GameState.GameOver)
            {
                string gameOverText = "Game Over! Press R to restart";
                Vector2 textPosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(gameOverText).X) / 2,
                    (GRID_HEIGHT * GRID_SIZE - gameFont.MeasureString(gameOverText).Y) / 2
                );
                spriteBatch.DrawString(gameFont, gameOverText, textPosition, Color.Red);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawSquare(Vector2 position, Color color)
        {
            spriteBatch.Draw(squareTexture,
                new Rectangle((int)(position.X * GRID_SIZE),
                            (int)(position.Y * GRID_SIZE),
                            GRID_SIZE - 1,
                            GRID_SIZE - 1),
                color);
        }
    }

    public class Button
    {
        public string Text { get; }
        public Vector2 Position { get; }
        public Action OnClick { get; }
        private SpriteFont font;
        private Rectangle bounds;

        public Button(string text, Vector2 position, Action onClick)
        {
            Text = text;
            Position = position;
            OnClick = onClick;
        }

        public void LoadContent(SpriteFont font)
        {
            this.font = font;
            this.bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)font.MeasureString(Text).X, (int)font.MeasureString(Text).Y);
        }

        public void Update()
        {
            MouseState mouseState = Mouse.GetState();
            if (bounds.Contains(mouseState.Position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                OnClick();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(font, Text, Position, Color.White);
        }
    }

    public enum PowerUpType
    {
        Speed,
        Shield,
        DoublePoints
    }
}