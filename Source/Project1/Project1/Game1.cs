using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D squareTexture;
        private SpriteFont gameFont;

        private const int GRID_SIZE = 20;
        private const int GRID_WIDTH = 40;
        private const int GRID_HEIGHT = 30;
        private const float INITIAL_MOVE_INTERVAL = 0.15f; // seconds between moves

        private List<Vector2> snakeBody;
        private Vector2 direction;
        private Vector2 foodPosition;
        private Vector2 powerUpPosition;
        private List<Vector2> walls;
        private PowerUpType currentPowerUp;
        private float moveTimer;
        private float currentMoveInterval;
        private float powerUpTimer;
        private bool hasShield;
        private bool doublePoints;
        private int score;
        private bool isGameOver;
        private KeyboardState previousKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = GRID_WIDTH * GRID_SIZE;
            graphics.PreferredBackBufferHeight = GRID_HEIGHT * GRID_SIZE;
        }

        protected override void Initialize()
        {
            InitializeGame();
            base.Initialize();
        }

        private void InitializeGame()
        {
            // Initialize snake in the middle of the screen
            snakeBody = new List<Vector2>
            {
                new Vector2(GRID_WIDTH / 2, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 2 - 1, GRID_HEIGHT / 2),
                new Vector2(GRID_WIDTH / 2 - 2, GRID_HEIGHT / 2)
            };

            direction = Vector2.UnitX;
            currentMoveInterval = INITIAL_MOVE_INTERVAL;
            moveTimer = 0;
            powerUpTimer = 0;
            score = 0;
            isGameOver = false;

            // Initialize walls
            walls = new List<Vector2>();
            InitializeWalls();

            PlaceFood();
            PlacePowerUp();
        }

        private void InitializeWalls()
        {
            // Create some wall obstacles
            for (int i = 5; i < 15; i++)
            {
                walls.Add(new Vector2(i, 5));
                walls.Add(new Vector2(i + 10, 15));
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a white square texture
            squareTexture = new Texture2D(GraphicsDevice, 1, 1);
            squareTexture.SetData(new[] { Color.White });

            // Load font - you'll need to add this to your Content project
            gameFont = Content.Load<SpriteFont>("GameFont");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (isGameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.R))
                    InitializeGame();
                return;
            }

            HandleInput();
            UpdateGame(gameTime);

            base.Update(gameTime);
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

            previousKeyboardState = keyboardState;
        }

        private void UpdateGame(GameTime gameTime)
        {
            moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update power-up timer
            if (hasShield || doublePoints || currentMoveInterval < INITIAL_MOVE_INTERVAL)
            {
                powerUpTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (powerUpTimer >= 10f) // Power-ups last 10 seconds
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

            // Wall collision
            if (head.X < 0 || head.X >= GRID_WIDTH || head.Y < 0 || head.Y >= GRID_HEIGHT ||
                walls.Any(w => w == head))
            {
                if (hasShield)
                {
                    hasShield = false;
                    snakeBody.RemoveAt(snakeBody.Count - 1);
                    return;
                }
                isGameOver = true;
                return;
            }

            // Self collision
            if (snakeBody.Skip(1).Any(segment => segment == head))
            {
                if (hasShield)
                {
                    hasShield = false;
                    snakeBody.RemoveAt(snakeBody.Count - 1);
                    return;
                }
                isGameOver = true;
                return;
            }

            // Food collision
            if (head == foodPosition)
            {
                score += doublePoints ? 2 : 1;
                snakeBody.Add(snakeBody[snakeBody.Count - 1]); // Grow snake
                PlaceFood();
            }

            // Power-up collision
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
            Random rand = new Random();
            do
            {
                foodPosition = new Vector2(
                    rand.Next(0, GRID_WIDTH),
                    rand.Next(0, GRID_HEIGHT)
                );
            } while (snakeBody.Contains(foodPosition) || walls.Contains(foodPosition));
        }

        private void PlacePowerUp()
        {
            Random rand = new Random();
            currentPowerUp = (PowerUpType)rand.Next(0, 3);
            do
            {
                powerUpPosition = new Vector2(
                    rand.Next(0, GRID_WIDTH),
                    rand.Next(0, GRID_HEIGHT)
                );
            } while (snakeBody.Contains(powerUpPosition) ||
                    walls.Contains(powerUpPosition) ||
                    powerUpPosition == foodPosition);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // Draw walls
            foreach (Vector2 wall in walls)
            {
                DrawSquare(wall, Color.Gray);
            }

            // Draw snake
            for (int i = 0; i < snakeBody.Count; i++)
            {
                Color snakeColor = hasShield ? Color.Gold : Color.Green;
                DrawSquare(snakeBody[i], i == 0 ? Color.LightGreen : snakeColor);
            }

            // Draw food
            DrawSquare(foodPosition, Color.Red);

            // Draw power-up
            Color powerUpColor = Color.White;
            switch (currentPowerUp)
            {
                case PowerUpType.Speed:
                    powerUpColor = Color.Blue;
                    break;
                case PowerUpType.Shield:
                    powerUpColor = Color.Yellow;
                    break;
                case PowerUpType.DoublePoints:
                    powerUpColor = Color.Purple;
                    break;
            }
            DrawSquare(powerUpPosition, powerUpColor);

            // Draw score and active power-ups
            string statusText = $"Score: {score}";
            if (hasShield) statusText += " SHIELD";
            if (currentMoveInterval < INITIAL_MOVE_INTERVAL) statusText += " SPEED";
            if (doublePoints) statusText += " 2X";

            spriteBatch.DrawString(gameFont, statusText, new Vector2(5, 5), Color.White);

            if (isGameOver)
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

    enum PowerUpType
    {
        Speed,
        Shield,
        DoublePoints
    }
}