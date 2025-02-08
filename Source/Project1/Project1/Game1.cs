using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

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
        private const float INITIAL_MOVE_INTERVAL = 0.15f;
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
        private int personalBest = 0;

        private enum GameState { Menu, Playing, GameOver }
        private GameState currentState = GameState.Menu;

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
        }

        protected override void Initialize()
        {
            random = new Random();
            snakeBody = new List<Vector2>();
            obstacles = new List<ObstacleInfo>();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            squareTexture = new Texture2D(GraphicsDevice, 1, 1);
            squareTexture.SetData(new[] { Color.White });
            gameFont = Content.Load<SpriteFont>("GameFont");
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            switch (currentState)
            {
                case GameState.Menu:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        StartGame();
                    }
                    break;

                case GameState.Playing:
                    if (isGameOver)
                    {
                        currentState = GameState.GameOver;
                        if (score > personalBest)
                        {
                            personalBest = score;
                        }
                    }
                    break;

                case GameState.GameOver:
                    if (Keyboard.GetState().IsKeyDown(Keys.R))
                    {
                        currentState = GameState.Menu;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        private void StartGame()
        {
            currentState = GameState.Playing;
            score = 0;
            isGameOver = false;
            snakeBody.Clear();
            snakeBody.Add(new Vector2(GRID_WIDTH / 2, GRID_HEIGHT / 2));
            direction = new Vector2(1, 0);
            obstacles.Clear();
            GenerateFood();
        }

        private void GenerateFood()
        {
            foodPosition = new Vector2(random.Next(0, GRID_WIDTH), random.Next(0, GRID_HEIGHT));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            if (currentState == GameState.Menu)
            {
                string menuText = $"Snake Game\nPersonal Best: {personalBest}\nPress ENTER to Start";
                Vector2 textSize = gameFont.MeasureString(menuText);
                Vector2 position = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    (GraphicsDevice.Viewport.Height - textSize.Y) / 2);
                spriteBatch.DrawString(gameFont, menuText, position, Color.White);
            }
            else if (currentState == GameState.GameOver)
            {
                string gameOverText = "Game Over! Press R to return to menu";
                Vector2 textSize = gameFont.MeasureString(gameOverText);
                Vector2 position = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    (GraphicsDevice.Viewport.Height - textSize.Y) / 2);
                spriteBatch.DrawString(gameFont, gameOverText, position, Color.Red);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }

    enum PowerUpType
    {
        Speed,
        Shield,
        DoublePoints
    }
}
