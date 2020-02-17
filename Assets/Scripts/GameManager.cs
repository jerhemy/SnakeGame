using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Experimental.AI;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Overheater.Snake
{
    public class GameManager : MonoBehaviour
    {
        public int maxHeight = 15;
        public int maxWidth = 17;

        public Color color1;

        public Color color2;
        public Color appleColor = Color.red;

        public Color playerColor = Color.black;

        public Transform cameraHolder;

        private GameObject mapObject;
        private GameObject playerObj;
        private GameObject tailParent;

        private GameObject appleObj;

        private Node playerNode;
        private Node appleNode;
        private Node prevPlayerNode;

        private SpriteRenderer mapRenderer;
        private Sprite playerSprite;

        private Node[,] grid;
        private List<Node> availableNodes = new List<Node>();
        private List<SpecialNode> playerTail = new List<SpecialNode>();
        private bool up, left, right, down;

        
        private Direction targetDirection;
        private Direction currentDirection;

        private int currentScore;
        private int highScore;

        public Text currentScoreText;
        public Text highScoreText;

        public bool IsGameOver;
        public bool IsFirstInput;

        public float moveRate = 0.5f;
        private float timer;

        public enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        public UnityEvent onStart;

        public UnityEvent onGameOver;

        public UnityEvent firstInput;

        public UnityEvent onScore;

        // Start is called before the first frame update
        void Start()
        {
            onStart.Invoke();
        }

        public void StartNewGame()
        {
            ClearReferences();
            CreateMap();
            PlacePlayer();
            PlaceCamera();
            CreateApple();
            targetDirection = Direction.Right;
            currentScore = 0;
            IsGameOver = false;
            UpdateScore();
        }

        public void ClearReferences()
        {
            Destroy(mapObject);
            Destroy(playerObj);
            Destroy(appleObj);
            foreach (var t in playerTail)
            {
                Destroy(t.obj);
            }
            playerTail.Clear();
            availableNodes.Clear();
            grid = null;
        }

        void Update()
        {
            if (IsGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    onStart.Invoke();
                }
                return;
            }


            GetInput();
            

            if (IsFirstInput)
            {
                SetPlayerDirection();
                timer += Time.deltaTime;
                if (timer > moveRate)
                {
                    timer = 0;
                    currentDirection = targetDirection;
                    MovePlayer();
                }
            }
            else
            {
                if (up || down || left || right)
                {
                    IsFirstInput = true;
                    firstInput.Invoke();
                }
            }
        }

        void GetInput()
        {
            up = Input.GetKeyDown(KeyCode.W);
            up = Input.GetKeyDown(KeyCode.UpArrow);
            down = Input.GetKeyDown(KeyCode.S);
            down = Input.GetKeyDown(KeyCode.DownArrow);
            left = Input.GetKeyDown(KeyCode.A);
            left = Input.GetKeyDown(KeyCode.LeftArrow);
            right = Input.GetKeyDown(KeyCode.D);
            right = Input.GetKeyDown(KeyCode.RightArrow);
        }

        void SetPlayerDirection()
        {
            if (up)
            {
                SetDirection(Direction.Up);
            }
            else if (down)
            {
                SetDirection(Direction.Down);


            } else if (left)
            {
                SetDirection(Direction.Left);
            }
            else if (right)
            {
                SetDirection(Direction.Right);
            }
        }

        void SetDirection(Direction d)
        {
            if (!IsOpposite(d))
            {
                targetDirection = d;
            }
        }

        void MovePlayer()
        {
            int x = 0;
            int y = 0;

            switch (currentDirection)
            {
                case Direction.Up:
                    y += 1;
                    break;
                case Direction.Down:
                    y -= 1;
                    break;
                case Direction.Left:
                    x -= 1;
                    break;
                case Direction.Right:
                    x += 1;
                    break;
            }

            Node targetNode = GetNode(playerNode.x + x, playerNode.y + y);

            if (targetNode == null)
            {
                onGameOver.Invoke();
            }
            else
            {
                if (IsTailNode(targetNode))
                {
                    onGameOver.Invoke();
                }
                else
                {
                    bool isScore = false;

                    if (targetNode == appleNode)
                    {
                        isScore = true;
                    }

                    Node prevNode = playerNode;

                    availableNodes.Add(prevNode);

                    if (isScore)
                    {
                        playerTail.Add(CreateTailNode(prevNode.x, prevNode.y));
                        availableNodes.Remove(prevNode);
                    }

                    MoveTail();

                    PlacePlayerObject(playerObj, targetNode.worldPosition);
                    playerNode = targetNode;
                    availableNodes.Remove(playerNode);

                    if (isScore)
                    {
                        
                        currentScore++;

                        if (currentScore >= highScore)
                        {
                            highScore = currentScore;
                        }
                        onScore.Invoke();

                        if (availableNodes.Count > 0)
                        {
                            RandomlyPlaceApple();
                        }
                        else
                        {
                            //Win
                        }
                    }

                }

            }
        }

        void MoveTail()
        {
            Node prevNode = null;

            for (int i = 0; i < playerTail.Count; i++)
            {
                SpecialNode p = playerTail[i];
                availableNodes.Add(p.node);

                if (i == 0)
                {
                    prevNode = p.node;
                    p.node = playerNode;

                }
                else
                {
                    Node prev = p.node;
                    p.node = prevNode;
                    prevNode = prev;
                }

                availableNodes.Remove(p.node);
                PlacePlayerObject(p.obj, p.node.worldPosition);
            }

     

        }
        // Update is called once per frame
        void CreateMap()
        {
            mapObject = new GameObject("Map");
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();

            grid = new Node[maxWidth, maxHeight];

            Texture2D txt = new Texture2D(maxWidth, maxHeight);

            #region Visual
            for (var x = 0; x < maxWidth; x++)
            {
                for (var y = 0; y < maxHeight; y++)
                {
                    Vector3 tp = Vector3.zero;
                    tp.x = x;
                    tp.y = y;

                    Node n = new Node() {x = x, y = y, worldPosition = tp};
                    grid[x, y] = n;
                    availableNodes.Add(n);

                    if (x % 2 != 0)
                    {
                        txt.SetPixel(x, y, y % 2 != 0 ? color1 : color2);
                    }
                    else
                    {
                        txt.SetPixel(x, y, y % 2 != 0 ? color2 : color1);
                    }
                }
            }
            #endregion

            txt.filterMode = FilterMode.Point;
            txt.Apply();
            Rect rect = new Rect(0,0, maxWidth, maxHeight);
            Sprite sprite = Sprite.Create(txt, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;
        }

        void PlacePlayer()
        {
            playerObj = new GameObject("Player");
            SpriteRenderer playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            playerSprite = CreateSprite(playerColor);
            playerRenderer.sprite = playerSprite;
            playerRenderer.sortingOrder = 1;

            playerNode = GetNode(3, 3);
            PlacePlayerObject(playerObj, playerNode.worldPosition);
            playerObj.transform.localScale = Vector3.one * 1.2f;
            tailParent = new GameObject("tailParent");
        }

        void PlaceCamera()
        {
            Node n = GetNode(maxWidth / 2, maxHeight / 2);
            Vector3 p = n.worldPosition;
            p += Vector3.one * 0.5f;
            cameraHolder.position = p;
        }

        void CreateApple()
        {
            appleObj = new GameObject("Apple");
            SpriteRenderer appleRenderer = appleObj.AddComponent<SpriteRenderer>();
            appleRenderer.sprite = CreateSprite(appleColor);
            appleRenderer.sortingOrder = 1;
            RandomlyPlaceApple();
        }

        #region Utilities

        public void UpdateScore()
        {
            currentScoreText.text = currentScore.ToString();
            highScoreText.text = highScore.ToString();
        }
        void RandomlyPlaceApple()
        {
            int ran = Random.Range(0, availableNodes.Count);
            Node n = availableNodes[ran];
            PlacePlayerObject(appleObj, n.worldPosition);
            appleNode = n;
            Debug.Log(appleNode);
        }

        Node GetNode(int x, int y)
        {
            if (x < 0 || x > maxWidth - 1 || y < 0 || y > maxHeight - 1)
                return null;

            return grid[x, y];
        }

        Sprite CreateSprite(Color targetColor)
        {
            Texture2D txt = new Texture2D(1, 1);
            txt.SetPixel(0,0, targetColor);
            txt.filterMode = FilterMode.Point;
            txt.Apply();
            Rect rect = new Rect(0, 0, 1, 1);
            return Sprite.Create(txt, rect, Vector2.one * .5f, 1, 0, SpriteMeshType.FullRect);

        }

        bool IsOpposite(Direction d)
        {
            switch (d)
            {
                default:
                case Direction.Up:
                    if (currentDirection == Direction.Down)
                        return true;
                    else
                        return false;
                case Direction.Down:
                    if (currentDirection == Direction.Up)
                        return true;
                    else
                        return false;
                case Direction.Left:
                    if (currentDirection == Direction.Right)
                        return true;
                    else
                        return false;
                case Direction.Right:
                    if (currentDirection == Direction.Left)
                        return true;
                    else
                        return false;
            }
        }

        bool IsTailNode(Node targetNode)
        {
            for (int i = 0; i < playerTail.Count; i++)
            {
                if (playerTail[i].node == targetNode)
                {
                    return true;
                }
            }

            return false;
        }

        void PlacePlayerObject(GameObject obj, Vector3 pos)
        {
            pos += Vector3.one * .5f;
            obj.transform.position = pos;
        }

        SpecialNode CreateTailNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x, y);
            s.obj = new GameObject("Tail");
            s.obj.transform.parent = tailParent.transform;
            s.obj.transform.position = s.node.worldPosition;
            s.obj.transform.localScale = Vector3.one * 0.95f;
            SpriteRenderer r = s.obj.AddComponent<SpriteRenderer>();
            r.sortingOrder = 1;
            r.sprite = playerSprite;

            return s;
        }

        public void GameOver()
        {
            IsGameOver = true;
            IsFirstInput = false;
        }

        #endregion
    }
}
