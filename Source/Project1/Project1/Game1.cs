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

    public enum GameMode
    {
        SinglePlayer,
        MultiPlayer
    }

    
    public enum PowerUpType
    {
        Slow,
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
        private GameMode currentGameMode;
        private Button startSinglePlayerButton;
        private Button startMultiPlayerButton;
        private Button exitButton;
        private Texture2D shieldTexture;

        private const int GRID_SIZE = 35;
        private const int GRID_WIDTH = 40;
        private const int GRID_HEIGHT = 25;
        private const float INITIAL_MOVE_INTERVAL = 0.15f;
        private const float OBSTACLE_SPAWN_INTERVAL = 3f;
        private const float OBSTACLE_LIFETIME = 6f;
        private const float POWER_UP_DURATION = 5f;

        private List<Vector2> snakeBody;
        private Vector2 direction;
        private bool hasShield;
        private bool doublePoints;
        private int score;

        private List<Vector2> snakeBody2;
        private Vector2 direction2;
        private bool hasShield2;
        private bool doublePoints2;
        private int score2;

        private Vector2 foodPosition;
        private Vector2 powerUpPosition;
        private List<ObstacleInfo> obstacles;
        private PowerUpType currentPowerUp;
        private float moveTimer;
        private float obstacleTimer;
        private float currentMoveInterval;
        private float powerUpTimer;
        private bool isGameOver;
        private Random random;
        private bool[] playerAlive; 
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
                if (pos.X < 0 || pos.X >= GRID_WIDTH || pos.Y < 0 || pos.Y >= GRID_HEIGHT)
                    return false;

               
                if (snakeBody.Contains(pos))
                    return false;

                if (currentGameMode == GameMode.MultiPlayer && snakeBody2.Contains(pos))
                    return false;

                
                if (obstacles.Any(o => o.Positions.Contains(pos)))
                    return false;

                
                if (pos == foodPosition || pos == powerUpPosition)
                    return false;

                
                if (Vector2.Distance(pos, snakeBody[0]) <= 3)
                    return false;

                if (currentGameMode == GameMode.MultiPlayer && Vector2.Distance(pos, snakeBody2[0]) <= 3)
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

            float buttonX = GRID_WIDTH / 2f * GRID_SIZE - 100;
            float buttonY = GRID_HEIGHT / 2f * GRID_SIZE - 80;

            startSinglePlayerButton = new Button("Single Player", new Vector2(buttonX, buttonY), () => StartGame(GameMode.SinglePlayer));
            startMultiPlayerButton = new Button("Multi Player", new Vector2(buttonX, buttonY + 70), () => StartGame(GameMode.MultiPlayer));
            exitButton = new Button("Exit", new Vector2(buttonX, buttonY + 140), ExitGame);

            base.Initialize();
        }

        private void StartGame(GameMode mode)
        {
            currentGameMode = mode;
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
                new Vector2(GRID_WIDTH / 3, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 3 - 1, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 3 - 2, GRID_HEIGHT / 2)
            };

            direction = Vector2.UnitX;
            hasShield = false;
            doublePoints = false;
            score = 0;

            
            if (currentGameMode == GameMode.MultiPlayer)
            {
                snakeBody2 = new List<Vector2>
                {
                    new Vector2(GRID_WIDTH * 2 / 3, GRID_HEIGHT / 2),
                    new Vector2(GRID_WIDTH * 2 / 3 + 1, GRID_HEIGHT / 2),
                    new Vector2(GRID_WIDTH * 2 / 3 + 2, GRID_HEIGHT / 2)
                };

                direction2 = -Vector2.UnitX;
                hasShield2 = false;
                doublePoints2 = false;
                score2 = 0;
            }

            playerAlive = new bool[] { true, currentGameMode == GameMode.MultiPlayer };

            currentMoveInterval = INITIAL_MOVE_INTERVAL;
            moveTimer = 0;
            obstacleTimer = 0;
            powerUpTimer = 0;
            isGameOver = false;
            obstacles = new List<ObstacleInfo>();

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
            shieldTexture = Content.Load<Texture2D>("shield");

            startSinglePlayerButton.LoadContent(gameFont, GraphicsDevice);
            startMultiPlayerButton.LoadContent(gameFont, GraphicsDevice);
            exitButton.LoadContent(gameFont, GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (currentGameState == GameState.MainMenu)
            {
                startSinglePlayerButton.Update();
                startMultiPlayerButton.Update();
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

                if (Keyboard.GetState().IsKeyDown(Keys.M))
                {
                    currentGameState = GameState.MainMenu;
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

            
            if (playerAlive[0])
            {
                if (keyboardState.IsKeyDown(Keys.Up) && direction != Vector2.UnitY)
                    direction = -Vector2.UnitY;
                else if (keyboardState.IsKeyDown(Keys.Down) && direction != -Vector2.UnitY)
                    direction = Vector2.UnitY;
                else if (keyboardState.IsKeyDown(Keys.Left) && direction != Vector2.UnitX)
                    direction = -Vector2.UnitX;
                else if (keyboardState.IsKeyDown(Keys.Right) && direction != -Vector2.UnitX)
                    direction = Vector2.UnitX;
            }

            
            if (currentGameMode == GameMode.MultiPlayer && playerAlive[1])
            {
                if (keyboardState.IsKeyDown(Keys.W) && direction2 != Vector2.UnitY)
                    direction2 = -Vector2.UnitY;
                else if (keyboardState.IsKeyDown(Keys.S) && direction2 != -Vector2.UnitY)
                    direction2 = Vector2.UnitY;
                else if (keyboardState.IsKeyDown(Keys.A) && direction2 != Vector2.UnitX)
                    direction2 = -Vector2.UnitX;
                else if (keyboardState.IsKeyDown(Keys.D) && direction2 != -Vector2.UnitX)
                    direction2 = Vector2.UnitX;
            }
        }

        private void UpdateGame(GameTime gameTime)
        {
            moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            
            if (hasShield || doublePoints || hasShield2 || doublePoints2 || currentMoveInterval != INITIAL_MOVE_INTERVAL)
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

                
                if (playerAlive[0])
                {
                    MoveSnake(snakeBody, direction);
                    CheckCollisions(0);
                }

                
                if (currentGameMode == GameMode.MultiPlayer && playerAlive[1])
                {
                    MoveSnake(snakeBody2, direction2);
                    CheckCollisions(1);
                }

                
                CheckGameOver();
            }
        }

        private void MoveSnake(List<Vector2> snake, Vector2 dir)
        {
            Vector2 newHead = snake[0] + dir;
            snake.Insert(0, newHead);
            snake.RemoveAt(snake.Count - 1);
        }

        private void CheckCollisions(int playerIndex)
        {
            List<Vector2> snake = playerIndex == 0 ? snakeBody : snakeBody2;
            Vector2 head = snake[0];
            bool hasShieldActive = playerIndex == 0 ? hasShield : hasShield2;

            
            bool hitWall = head.X < 0 || head.X >= GRID_WIDTH || head.Y < 0 || head.Y >= GRID_HEIGHT;
            bool hitObstacle = obstacles.Any(o => o.Positions.Contains(head));

            bool hitOtherSnake = false;
            if (currentGameMode == GameMode.MultiPlayer)
            {
                List<Vector2> otherSnake = playerIndex == 0 ? snakeBody2 : snakeBody;
                hitOtherSnake = playerAlive[1 - playerIndex] && otherSnake.Contains(head);
            }

            
            bool hitSelf = snake.Skip(1).Any(segment => segment == head);

            if (hitWall || hitObstacle || hitOtherSnake || hitSelf)
            {
                if (hasShieldActive)
                {
                    
                    if (playerIndex == 0)
                        hasShield = false;
                    else
                        hasShield2 = false;

                   
                    if (snake.Count > 3)
                        snake.RemoveAt(snake.Count - 1);
                    return;
                }

                
                playerAlive[playerIndex] = false;
            }

            
            if (head == foodPosition)
            {
                
                if (playerIndex == 0)
                    score += doublePoints ? 2 : 1;
                else
                    score2 += doublePoints2 ? 2 : 1;

                
                snake.Add(snake[snake.Count - 1]);
                PlaceFood();
            }

            
            if (head == powerUpPosition)
            {
                ApplyPowerUp(playerIndex);
                PlacePowerUp();
            }
        }

        private void CheckGameOver()
        {
            
            if (currentGameMode == GameMode.SinglePlayer && !playerAlive[0])
            {
                isGameOver = true;
                currentGameState = GameState.GameOver;
            }
            else if (currentGameMode == GameMode.MultiPlayer && !playerAlive[0] && !playerAlive[1])
            {
                isGameOver = true;
                currentGameState = GameState.GameOver;
            }
        }

        private void ApplyPowerUp(int playerIndex)
        {
            powerUpTimer = 0;
            switch (currentPowerUp)
            {
                case PowerUpType.Slow:
                    currentMoveInterval = INITIAL_MOVE_INTERVAL * 1.5f;
                    break;
                case PowerUpType.Speed:
                    currentMoveInterval = INITIAL_MOVE_INTERVAL / 1.5f;
                    break;
                case PowerUpType.Shield:
                    if (playerIndex == 0)
                        hasShield = true;
                    else
                        hasShield2 = true;
                    break;
                case PowerUpType.DoublePoints:
                    if (playerIndex == 0)
                        doublePoints = true;
                    else
                        doublePoints2 = true;
                    break;
            }
        }

        private void ResetPowerUps()
        {
            currentMoveInterval = INITIAL_MOVE_INTERVAL;
            hasShield = false;
            doublePoints = false;
            hasShield2 = false;
            doublePoints2 = false;
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
                    (currentGameMode == GameMode.MultiPlayer && snakeBody2.Contains(foodPosition)) ||
                    obstacles.Any(o => o.Positions.Contains(foodPosition)));
        }

        private void PlacePowerUp()
        {
            currentPowerUp = (PowerUpType)random.Next(0, 4);
            do
            {
                powerUpPosition = new Vector2(
                    random.Next(0, GRID_WIDTH),
                    random.Next(0, GRID_HEIGHT)
                );
            } while (snakeBody.Contains(powerUpPosition) ||
                    (currentGameMode == GameMode.MultiPlayer && snakeBody2.Contains(powerUpPosition)) ||
                    obstacles.Any(o => o.Positions.Contains(powerUpPosition)) ||
                    powerUpPosition == foodPosition);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            if (currentGameState == GameState.MainMenu)
            {
                string titleText = "Street Snake";
                Vector2 titlePosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(titleText).X) / 2,
                    (GRID_HEIGHT * GRID_SIZE - gameFont.MeasureString(titleText).Y) / 2 - 150
                );
                spriteBatch.DrawString(gameFont, titleText, titlePosition, Color.Green);

                
                startSinglePlayerButton.Draw(spriteBatch);
                startMultiPlayerButton.Draw(spriteBatch);
                exitButton.Draw(spriteBatch);
            }
            else if (currentGameState == GameState.Playing)
            {
                
                foreach (var obstacle in obstacles)
                {
                    foreach (var position in obstacle.Positions)
                    {
                        DrawSquare(position, obstacle.Color);
                    }
                }

                
                if (playerAlive[0])
                {
                    for (int i = 0; i < snakeBody.Count; i++)
                    {
                        Color snakeColor = hasShield ? Color.Gold : Color.Green;
                        DrawSquare(snakeBody[i], i == 0 ? Color.LightGreen : snakeColor);
                    }
                }

                
                if (currentGameMode == GameMode.MultiPlayer && playerAlive[1])
                {
                    for (int i = 0; i < snakeBody2.Count; i++)
                    {
                        Color snakeColor = hasShield2 ? Color.Gold : Color.Cyan;
                        DrawSquare(snakeBody2[i], i == 0 ? Color.LightBlue : snakeColor);
                    }
                }

               
                DrawSquare(foodPosition, Color.Red);

               
                DrawPowerUp(powerUpPosition, currentPowerUp);

                string p1StatusText = $"P1 Score: {score}";
                if (hasShield) p1StatusText += " SHIELD";
                if (doublePoints) p1StatusText += " 2X";
                spriteBatch.DrawString(gameFont, p1StatusText, new Vector2(5, 5), playerAlive[0] ? Color.White : Color.Red);

                
                if (currentGameMode == GameMode.MultiPlayer)
                {
                    string p2StatusText = $"P2 Score: {score2}";
                    if (hasShield2) p2StatusText += " SHIELD";
                    if (doublePoints2) p2StatusText += " 2X";
                    Vector2 textSize = gameFont.MeasureString(p2StatusText);
                    spriteBatch.DrawString(gameFont, p2StatusText,
                        new Vector2(GRID_WIDTH * GRID_SIZE - textSize.X - 5, 5),
                        playerAlive[1] ? Color.White : Color.Red);
                }

                
                string speedText = currentMoveInterval < INITIAL_MOVE_INTERVAL ? "SPEED" :
                                  (currentMoveInterval > INITIAL_MOVE_INTERVAL ? "SLOW" : "");
                if (!string.IsNullOrEmpty(speedText))
                {
                    Vector2 textSize = gameFont.MeasureString(speedText);
                    spriteBatch.DrawString(gameFont, speedText,
                        new Vector2((GRID_WIDTH * GRID_SIZE - textSize.X) / 2, 5), Color.Yellow);
                }
            }
            else if (currentGameState == GameState.GameOver)
            {
                string gameOverText = "Game Over!";
                Vector2 textPosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(gameOverText).X) / 2,
                    (GRID_HEIGHT * GRID_SIZE - gameFont.MeasureString(gameOverText).Y) / 2 - 60
                );
                spriteBatch.DrawString(gameFont, gameOverText, textPosition, Color.Red);

                
                if (currentGameMode == GameMode.MultiPlayer)
                {
                    string winnerText;
                    Color winnerColor;

                    if (score > score2)
                    {
                        winnerText = "Player 1 Wins!";
                        winnerColor = Color.Green;
                    }
                    else if (score2 > score)
                    {
                        winnerText = "Player 2 Wins!";
                        winnerColor = Color.Cyan;
                    }
                    else
                    {
                        winnerText = "It's a Tie!";
                        winnerColor = Color.Yellow;
                    }

                    Vector2 winnerPosition = new Vector2(
                        (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(winnerText).X) / 2,
                        textPosition.Y + gameFont.MeasureString(gameOverText).Y + 20
                    );
                    spriteBatch.DrawString(gameFont, winnerText, winnerPosition, winnerColor);
                }

                
                string p1ScoreText = $"Player 1 Score: {score}";
                Vector2 p1ScorePosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(p1ScoreText).X) / 2,
                    textPosition.Y + gameFont.MeasureString(gameOverText).Y +
                    (currentGameMode == GameMode.MultiPlayer ? 60 : 20)
                );
                spriteBatch.DrawString(gameFont, p1ScoreText, p1ScorePosition, Color.White);

                if (currentGameMode == GameMode.MultiPlayer)
                {
                    string p2ScoreText = $"Player 2 Score: {score2}";
                    Vector2 p2ScorePosition = new Vector2(
                        (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(p2ScoreText).X) / 2,
                        p1ScorePosition.Y + gameFont.MeasureString(p1ScoreText).Y + 20
                    );
                    spriteBatch.DrawString(gameFont, p2ScoreText, p2ScorePosition, Color.White);
                }

                
                string restartText = "Press R to restart or M for menu";
                Vector2 restartPosition = new Vector2(
                    (GRID_WIDTH * GRID_SIZE - gameFont.MeasureString(restartText).X) / 2,
                    GRID_HEIGHT * GRID_SIZE - 60
                );
                spriteBatch.DrawString(gameFont, restartText, restartPosition, Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawPowerUp(Vector2 position, PowerUpType powerUpType)
        {
            Rectangle rect = new Rectangle(
                (int)(position.X * GRID_SIZE),
                (int)(position.Y * GRID_SIZE),
                GRID_SIZE - 1,
                GRID_SIZE - 1
            );

            switch (powerUpType)
            {
                case PowerUpType.Shield:
                    
                    spriteBatch.Draw(shieldTexture, rect, Color.White);
                    break;
                case PowerUpType.Slow:
                    DrawSquare(position, Color.Pink);
                    break;
                case PowerUpType.Speed:
                    DrawSquare(position, Color.Blue);
                    break;
                case PowerUpType.DoublePoints:
                    DrawSquare(position, Color.Purple);
                    break;
                default:
                    DrawSquare(position, Color.White);
                    break;
            }
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
        private Texture2D buttonTexture;

        public Button(string text, Vector2 position, Action onClick)
        {
            Text = text;
            Position = position;
            OnClick = onClick;
        }

        public void LoadContent(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            this.font = font;
            this.bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)font.MeasureString(Text).X + 40, (int)font.MeasureString(Text).Y + 20);

            buttonTexture = new Texture2D(graphicsDevice, 1, 1);
            buttonTexture.SetData(new[] { Color.White });
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
            MouseState mouseState = Mouse.GetState();
            bool isHovering = bounds.Contains(mouseState.Position);

            
            Color bgColor = isHovering ? Color.DarkGray : Color.Gray;
            spriteBatch.Draw(buttonTexture, bounds, bgColor);

            Vector2 textSize = font.MeasureString(Text);
            Vector2 textPosition = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, Text, textPosition, Color.White);
        }
    }
}
