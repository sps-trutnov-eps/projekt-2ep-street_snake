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

    // Moving the PowerUpType enum to the top level for better organization
    public enum PowerUpType
    {
        Speed,
        Shield,
        DoublePoints
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
        private const float INITIAL_MOVE_INTERVAL = 0.15f;
        private const float OBSTACLE_SPAWN_INTERVAL = 3f;
        private const float OBSTACLE_LIFETIME = 6f;
        private const float POWER_UP_DURATION = 5f;

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

        public enum ObstacleShape
        {
            Single,
            LShape,
            Tree,
            Wall,
            Cross
        }

        public class ObstacleInfo
        {
            public List<Vector2> Positions { get; set; }
            public float TimeRemaining { get; set; }
            public Color Color { get; set; }

            public ObstacleInfo(List<Vector2> positions, float lifetime)
            {
                Positions = positions;
                TimeRemaining = lifetime;
                Color = Color.Gray;
            }
        }

        private List<Vector2> CreateObstacleShape(Vector2 startPos, ObstacleShape shape)
        {
            List<Vector2> positions = new List<Vector2>();

            switch (shape)
            {
                case ObstacleShape.Single:
                    positions.Add(startPos);
                    break;

                case ObstacleShape.LShape:
                    positions.Add(startPos);
                    positions.Add(new Vector2(startPos.X + 1, startPos.Y));
                    positions.Add(new Vector2(startPos.X + 1, startPos.Y + 1));
                    break;

                case ObstacleShape.Tree:
                    positions.Add(startPos);
                    positions.Add(new Vector2(startPos.X, startPos.Y + 1));
                    positions.Add(new Vector2(startPos.X - 1, startPos.Y));
                    positions.Add(new Vector2(startPos.X + 1, startPos.Y));
                    break;

                case ObstacleShape.Wall:
                    for (int i = 0; i < 4; i++)
                    {
                        positions.Add(new Vector2(startPos.X + i, startPos.Y));
                    }
                    break;

                case ObstacleShape.Cross:
                    positions.Add(startPos);
                    positions.Add(new Vector2(startPos.X - 1, startPos.Y));
                    positions.Add(new Vector2(startPos.X + 1, startPos.Y));
                    positions.Add(new Vector2(startPos.X, startPos.Y - 1));
                    positions.Add(new Vector2(startPos.X, startPos.Y + 1));
                    break;
            }

            return positions;
        }

        private bool IsValidObstaclePosition(List<Vector2> positions)
        {
            foreach (var pos in positions)
            {
                // Check if position is within grid bounds
                if (pos.X < 0 || pos.X >= GRID_WIDTH || pos.Y < 0 || pos.Y >= GRID_HEIGHT)
                    return false;

                // Check if position overlaps with snake
                if (snakeBody.Contains(pos))
                    return false;

                // Check if position overlaps with existing obstacles
                if (obstacles.Any(o => o.Positions.Contains(pos)))
                    return false;

                // Check if position overlaps with food or power-up
                if (pos == foodPosition || pos == powerUpPosition)
                    return false;

                // Check if position is too close to snake head
                if (Vector2.Distance(pos, snakeBody[0]) <= 3)
                    return false;
            }

            return true;
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
            int maxAttempts = 50;
            int attempts = 0;

            ObstacleShape shape = (ObstacleShape)random.Next(0, 5);

            while (attempts < maxAttempts)
            {
                Vector2 startPos = new Vector2(
                    random.Next(0, GRID_WIDTH),
                    random.Next(0, GRID_HEIGHT)
                );

                List<Vector2> obstaclePositions = CreateObstacleShape(startPos, shape);

                if (IsValidObstaclePosition(obstaclePositions))
                {
                    obstacles.Add(new ObstacleInfo(obstaclePositions, OBSTACLE_LIFETIME));
                    return;
                }

                attempts++;
            }
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
                if (powerUpTimer >= POWER_UP_DURATION)
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

            // Check wall collisions
            if (head.X < 0 || head.X >= GRID_WIDTH || head.Y < 0 || head.Y >= GRID_HEIGHT ||
                obstacles.Any(o => o.Positions.Contains(head)))
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
                    currentMoveInterval = INITIAL_MOVE_INTERVAL / 2.5f;
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
                     obstacles.Any(o => o.Positions.Contains(foodPosition)));
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
                     obstacles.Any(o => o.Positions.Contains(powerUpPosition)) ||
                     powerUpPosition == foodPosition);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            if (currentGameState == GameState.MainMenu)
            {
                // Add main menu drawing code
                string titleText = "Street Snake";
                Vector2 titlePosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(titleText).X) / 2,
                    (GRID_HEIGHT * GRID_SIZE - gameFont.MeasureString(titleText).Y) / 2 - 100
                );
                spriteBatch.DrawString(gameFont, titleText, titlePosition, Color.Green);

                // Draw the buttons
                startButton.Draw(spriteBatch);
                exitButton.Draw(spriteBatch);
            }
            else if (currentGameState == GameState.Playing)
            {
                // Draw obstacles
                foreach (var obstacle in obstacles)
                {
                    foreach (var position in obstacle.Positions)
                    {
                        DrawSquare(position, obstacle.Color);
                    }
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

                // Display final score
                string scoreText = $"Final Score: {score}";
                Vector2 scorePosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(scoreText).X) / 2,
                    textPosition.Y + gameFont.MeasureString(gameOverText).Y + 20
                );
                spriteBatch.DrawString(gameFont, scoreText, scorePosition, Color.White);
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
}