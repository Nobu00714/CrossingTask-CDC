using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using uWintab;
using UnityEngine.SceneManagement;

public class TaskController : MonoBehaviour
{
    [SerializeField] public int Participant;
    [SerializeField] public Bias bias;
    [SerializeField] private Team team;
    [SerializeField] public Device device;
    [SerializeField] private bool practice;
    [SerializeField] private int set;
    [SerializeField] public int taskNum;
    [SerializeField] public int biasNum;
    [SerializeField] private int AmplitudeSum;
    [SerializeField] private int WidthSum;
    [SerializeField] private int nowTaskAmplitude;
    [SerializeField] private int nowTaskWidth;
    [SerializeField] GameObject start;
    [SerializeField] GameObject goal;
    [SerializeField] GameObject button;
    [SerializeField] private AudioClip correctAudio;
    [SerializeField] private AudioClip wrongAudio;
    [SerializeField] private GameObject cursor;
    private Tablet tablet;
    private bool taskReady;
    private bool taskStarted;
    private bool startCrossed;
    private bool goalCrossed;
    private bool nextButtonPressed;
    private bool mousePress;
    private int[] taskAmplitude;
    private int[] taskWidth;
    private List<int> taskList;
    private StreamWriter swPos;
    private StreamWriter swMT;
    private float taskStartTime;
    private float taskFinishTime;
    private float mouseX;
    private float mouseY;   
    private bool falseStart = false;
    private SpriteRenderer startRenderer;
    private SpriteRenderer goalRenderer;
    private AudioSource audioSource;
    private float Ae;
    private float AeOld;
    private Vector2 StartPoint;
    private Vector2 EndPoint;
    private float We;
    private float mouseXprev;
    private float mouseYprev;
    private int taskClear;
    private int sameTaskNum;
    RectTransform rectTransform;
    private bool buttonPressable = true;
    GameObject trajestory;
    GameObject trajestoryLine;
    [SerializeField] GameObject cursorTrajestoryParent;
    [SerializeField] LineRenderer lineRenderer;
    GameObject LineInstance;
    private bool first = true;
    private bool CSVUpdated = false;
    private bool firstPenTouch =true;
    private bool firstPenRelease =true;
    private List<Vector3> trajestoryList = new List<Vector3>();
    public enum Bias
    {
        Fast,
        Neutral,
        Accurate
    }
    public enum Team
    {
        FastToAccurate,
        AccurateToFast
    }
    public enum Device
    {
        Mouse,
        Pen
    }
    void Start()
    {
        Application.targetFrameRate = 120;
        startRenderer = start.GetComponent<SpriteRenderer>();
        goalRenderer = goal.GetComponent<SpriteRenderer>();
        audioSource = this.GetComponent<AudioSource>();
        rectTransform = button.GetComponent<RectTransform>();
        tablet = this.GetComponent<Tablet>();
        taskList = new List<int>();
        for(int i=0; i<AmplitudeSum*WidthSum; i++)
        {
            taskList.Add(i);
        }
        ShuffleList(taskList);

        taskAmplitude = new int[3];
        taskAmplitude[0] = 188;
        taskAmplitude[1] = 516;
        taskAmplitude[2] = 828;

        taskWidth = new int[5];
        taskWidth[0] = 8;
        taskWidth[1] = 14;
        taskWidth[2] = 26;
        taskWidth[3] = 46;
        taskWidth[4] = 94;

        taskUpdate();
        
        Cursor.visible = false;
        trajestory = (GameObject)Resources.Load("CursorTrajestory");
        trajestoryLine = (GameObject)Resources.Load("Line");

        bias = VariableManager.bias;
        biasNum = VariableManager.biasNum;
        Participant = VariableManager.Participant;
        taskNum = VariableManager.taskNum;
        VariableManager.MTSum = 0;
        swMT = VariableManager.swMT;
        VariableManager.ERSum = 0;
    }
    void OnApplicationQuit()
    {
        swMT.Close();
    }
    void Update()
    {
        DecideCursorPos();
        DrawCursorTrajestory();
        if(taskNum == 0)
        {
            if(first)
            {
                makeMTCSV();
                first = false;
                VariableManager.swMT = swMT;
            }
        }

        float startX = nowTaskAmplitude/2;
        float startYUp = nowTaskWidth/2;
        float startYBottom = -nowTaskWidth/2;
        float goalX = -nowTaskAmplitude/2;
        float goalYUp = nowTaskWidth/2;
        float goalYBottom = -nowTaskWidth/2;

        //ネクストボタンが押され，マウスがスタートより左に戻ったら準備完了
        if(nextButtonPressed)
        {
            if(mouseX>startX)
            {
                nextButtonPressed = false;
                taskReady = true;
                CSVUpdated = false;
            }
        }
        if(taskReady && mousePress)
        {
            //スタート地点を超えたら
            if(mouseX<=startX && mouseXprev>=startX)
            {
                startCrossed = true;

                //スタート地点を2点（）現在位置と1フレーム前位置から計測
                float YDist = Mathf.Abs(mouseYprev-mouseY);
                float XDistToStart = Mathf.Abs(mouseX-startX);
                float XDist = Mathf.Abs(mouseX-mouseXprev);
                float PositiveNegative = Mathf.Sign(mouseYprev-mouseY);
                StartPoint = new Vector2(startX, mouseY+PositiveNegative*YDist*(XDistToStart/XDist));
                /*Debug.Log("mouseX"+mouseX);
                Debug.Log("mouseY"+mouseY);
                Debug.Log("mouseXprev"+mouseXprev);
                Debug.Log("mouseYprev"+mouseYprev);
                Debug.Log("startX"+StartPoint.x);
                Debug.Log("startY"+StartPoint.y);*/
                if(!falseStart)
                {
                    //スタートターゲットを通過したら
                    if(StartPoint.y>=startYBottom && StartPoint.y<=startYUp)
                    {
                        Ae = 0f;
                        startRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                        taskStartTime = Time.time;
                        taskStarted = true;
                        taskReady = false;
                    }
                    //ターゲット外を通過したら
                    else
                    {
                        startRenderer.color = new Color(1f,0f,0f,1f);   //赤色にする
                        falseStart = true;
                    }
                }
            }
            //スタート地点より右に戻ったら
            if(mouseX>startX)
            {
                startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                falseStart = false;
            }
        } 
        if(taskStarted && mousePress)
        { 
            Ae += Mathf.Sqrt(Mathf.Pow(mouseX-mouseXprev,2)+Mathf.Pow(mouseY-mouseYprev,2));
            //ゴール地点を超えたら
            if(mouseX<goalX)
            {
                float YDist = Mathf.Abs(mouseYprev-mouseY);
                float XDistToStart = Mathf.Abs(mouseX-goalX);
                float XDist = Mathf.Abs(mouseX-mouseXprev);
                float PositiveNegative = Mathf.Sign(mouseYprev-mouseY);
                EndPoint = new Vector2(goalX, mouseY+PositiveNegative*YDist*(XDistToStart/XDist));
                Debug.Log("mouseX"+mouseX);
                Debug.Log("mouseY"+mouseY);
                Debug.Log("mouseXprev"+mouseXprev);
                Debug.Log("mouseYprev"+mouseYprev);
                Debug.Log("endX"+EndPoint.x);
                Debug.Log("endY"+EndPoint.y);

                taskFinishTime = Time.time;
                taskStarted = false;
                goalCrossed = true;
                We = EndPoint.y;
                //ゴールターゲットを通過したら
                if(EndPoint.y>=goalYBottom && EndPoint.y<=goalYUp)
                {
                    goalRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                    //クリアを記録
                    taskClear = 1;
                    audioSource.PlayOneShot(correctAudio);
                    
                }
                //ターゲット外を通過したら
                else
                {
                    goalRenderer.color = new Color(1f,0f,0f,1f);   //緑色にする
                    //エラーを記録
                    taskClear = 0;
                    audioSource.PlayOneShot(wrongAudio);
                }
                rectTransform.anchoredPosition = new Vector3(-750,UnityEngine.Random.Range(-282,282),0);
            }
        }
        if(device == Device.Pen)
        {
            //ペンを付けた時
            if(tablet.pressure>0.001)
            {
                firstPenRelease = true;
                if(firstPenTouch)
                {
                    firstPenTouch = false;
                    LineInstance = (GameObject)Instantiate(trajestoryLine, Vector3.zero, Quaternion.identity);
                    LineInstance.transform.parent = cursorTrajestoryParent.transform;
                    if(taskReady)
                    {
                        mousePress = true;
                        makePosCSV();
                    }
                    if(buttonPressable && mouseX>=rectTransform.anchoredPosition.x-250 && mouseX<=rectTransform.anchoredPosition.x+250 && mouseY>=rectTransform.anchoredPosition.y-250 && mouseY<=rectTransform.anchoredPosition.y+250)
                    {
                        startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        if(goalCrossed)
                        {
                            taskNum++;
                            taskUpdate();
                        }
                        nextButtonPressed = true;
                        button.SetActive(false);
                        buttonPressable = false;
                        goalCrossed = false;
                    }
                }
            }
            //ペンを離したとき
            if(tablet.pressure<0.001)
            { 
                firstPenTouch = true;
                if(firstPenRelease)
                {
                    firstPenRelease =false;
                    trajestoryList.Clear();
                    //ゴールしてから離したら
                    if(startCrossed && goalCrossed)
                    {
                        if(!CSVUpdated)
                        {
                            //クリアかエラーを記録
                            updateMTCSV(taskClear);
                            //ボタンをランダム位置に出現
                            button.SetActive(true);
                            buttonPressable = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            sameTaskNum = 0;
                            CSVUpdated = true;
                            if(taskClear == 0)
                            {
                                VariableManager.ERSum++;
                                print(VariableManager.ERSum);
                            }
                        }
                    }

                    //ゴールせずに離したら（スタートは成功）
                    if(startCrossed && !goalCrossed && !falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //ゴール前に離したというデータを記録
                            updateMTCSV(2);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            
                        }
                    }
                    //ゴールせずに離したら（スタートも失敗）
                    if(startCrossed && !goalCrossed && falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(3);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }
                    //スタートせずに離したら
                    if(!startCrossed && taskReady)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(4);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }
                    
                    foreach ( Transform child in cursorTrajestoryParent.transform )
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                    mousePress = false;
                    startCrossed = false;
                }
            }
        }
        if(device == Device.Mouse)
        {
            if(Input.GetMouseButtonDown(0))
            {
                LineInstance = (GameObject)Instantiate(trajestoryLine, Vector3.zero, Quaternion.identity);
                LineInstance.transform.parent = cursorTrajestoryParent.transform;
                if(taskReady)
                {
                    mousePress = true;
                    makePosCSV();
                }
                if(buttonPressable && mouseX>=rectTransform.anchoredPosition.x-250 && mouseX<=rectTransform.anchoredPosition.x+250 && mouseY>=rectTransform.anchoredPosition.y-250 && mouseY<=rectTransform.anchoredPosition.y+250)
                {
                    startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                    goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                    if(goalCrossed)
                    {
                        taskNum++;
                        taskUpdate();
                    }
                    nextButtonPressed = true;
                    button.SetActive(false);
                    buttonPressable = false;
                    goalCrossed = false;
                }
            }
            if(Input.GetMouseButtonUp(0))
            {  
                trajestoryList.Clear();
                //ゴールしてから離したら
                if(goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        //クリアかエラーを記録
                        updateMTCSV(taskClear);
                        //ボタンをランダム位置に出現
                        button.SetActive(true);
                        buttonPressable = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        sameTaskNum = 0;
                        CSVUpdated = true;
                    }
                }

                //ゴールせずに離したら
                if(startCrossed && !goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        startRenderer.color = new Color(1f,1f,1f,1f);
                        //ゴール前に離したというデータを記録
                        updateMTCSV(2);
                        audioSource.PlayOneShot(wrongAudio);
                        //もう一度同じタスクを実行
                        sameTaskNum++;
                        nextButtonPressed = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        taskStarted = false;
                        CSVUpdated = true;
                    }
                }
                foreach ( Transform child in cursorTrajestoryParent.transform )
                {
                    GameObject.Destroy(child.gameObject);
                }
                mousePress = false;
                startCrossed = false;
            }
        }
        
        if(mousePress)
        {
            updatePosCSV();
        }
        ChangeBias();
        ShowSetResult();
        FinishExperiment();
        mouseXprev = mouseX;
        mouseYprev = mouseY;
    }
    //ボタンが押されたら
    public void OnClick()
    {
        startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
        goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
        if(goalCrossed)
        {
            taskNum++;
            taskUpdate();
        }
        nextButtonPressed = true;
        button.SetActive(false);
        goalCrossed = false;
    }
    //カーソルの位置を決定
    private void DecideCursorPos()
    {
        if(device == Device.Pen)
        {
            mouseX = tablet.x * Screen.currentResolution.width -Screen.currentResolution.width/2;
            mouseY = tablet.y * Screen.currentResolution.height - Screen.currentResolution.height/2;
        }
        if(device == Device.Mouse)
        {
            mouseX = Input.mousePosition.x - Screen.currentResolution.width/2;
            mouseY = Input.mousePosition.y - Screen.currentResolution.height/2;
        }
        cursor.GetComponent<RectTransform>().anchoredPosition = new Vector3(mouseX, mouseY, 1);
        //Debug.Log("X:"+mouseX+"Y:"+mouseY);
    }
    //カーソルの軌跡を描画
    private void DrawCursorTrajestory()
    {
        if(mousePress)
        {
            //GameObject instance = (GameObject)Instantiate(trajestory, cursor.GetComponent<RectTransform>().anchoredPosition, Quaternion.identity);
            //instance.transform.parent = cursorTrajestoryParent.transform;
            LineRenderer lineRenderer = LineInstance.GetComponent<LineRenderer>();
            trajestoryList.Add(cursor.GetComponent<RectTransform>().anchoredPosition);
            var positions = new Vector3[trajestoryList.Count];
            for(int i=0; i<trajestoryList.Count; i++)
            {
                positions[i] = trajestoryList[i];
            }
            lineRenderer.positionCount = trajestoryList.Count;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0,0,1,1);
            lineRenderer.endColor = new Color(0,0,1,1);
            lineRenderer.SetPositions(positions);
        }
    }
    private void ShowSetResult()
    {
        if(taskNum%15 == 0 && taskNum != 0 && !VariableManager.resultCheck)
        {
            VariableManager.taskNum = taskNum;
            SceneManager.LoadScene("SetResultScene");
        }
        if(taskNum%15 == 1)
        {
            VariableManager.resultCheck = false;
        }
    }
    //バイアスを変更
    private void ChangeBias()
    {
        if(taskNum>=set*WidthSum*AmplitudeSum)
        {
            taskNum = 0;
            first = true;
            biasNum++;
            VariableManager.biasNum = biasNum;
            swMT.Close();
            if(team == Team.FastToAccurate)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
            }
            if(team == Team.AccurateToFast)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
            }
        }
    }
    //タスクがすべて終わったら終了
    private void FinishExperiment()
    {
        if(biasNum>2)
        {   
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
            #else
                Application.Quit();//ゲームプレイ終了
            #endif
        }
    }
    //タスクの順番をシャッフル
    private void ShuffleList(List<int> list)
    {
        int tmp;
        int rndNum;
        for(int i=list.Count-1; i>1; i--)
        {
            rndNum = UnityEngine.Random.Range(0,i);
            tmp = list[rndNum];
            list[rndNum] = list[i];
            list[i] = tmp;
        }
    }
    //タスクの更新
    private void taskUpdate()
    {
        nowTaskWidth = taskWidth[taskList[taskNum%(AmplitudeSum*WidthSum)]%WidthSum];
        nowTaskAmplitude = taskAmplitude[taskList[taskNum%(AmplitudeSum*WidthSum)]/WidthSum];
        start.transform.localScale = new Vector3(3,nowTaskWidth,1);
        goal.transform.localScale = new Vector3(3,nowTaskWidth,1);
        start.transform.position = new Vector3((nowTaskAmplitude/2)-1,nowTaskWidth/2,0);
        goal.transform.position = new Vector3(-(nowTaskAmplitude/2)-1,nowTaskWidth/2,0);
    }
    //マウス座標を保存するCSVを作成
    private void makePosCSV()
    {
        if(practice)
        {
            swPos = new StreamWriter(@"PracticePos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swPos = new StreamWriter(@"Pos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+"Num"+sameTaskNum+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "長さ", "幅", "時間", "x座標", "y座標"};
        string s2 = string.Join(",", s1);
        swPos.WriteLine(s2);
    }
    //フレームごとにマウス座標を保存
    private void updatePosCSV()
    {
        string[] s1 = {Participant.ToString(),nowTaskAmplitude.ToString(),nowTaskWidth.ToString(),(Time.time-taskStartTime).ToString(),mouseX.ToString(),mouseY.ToString()};
        string s2 = string.Join(",",s1);
        if(swPos!=null)
        {
            swPos.WriteLine(s2);
        }
    }
    //操作時間を保存するCSVを作成
    private void makeMTCSV()
    {
        if(practice)
        {
            swMT = new StreamWriter(@"PracticeMT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swMT = new StreamWriter(@"MT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "セット", "試行", "長さ", "幅","バイアス", "操作時間", "終点分布","旧Ae","軌跡総距離","クリア" };
        string s2 = string.Join(",", s1);
        swMT.WriteLine(s2);
    }
    //タスクのクリアごとに操作時間を保存
    private void updateMTCSV(int clear)
    {
        AeOld = Mathf.Abs(StartPoint.x-EndPoint.x);
        string[] s1 = {Participant.ToString(), (taskNum/(AmplitudeSum*WidthSum)).ToString(), (taskNum%(AmplitudeSum*WidthSum)).ToString(), nowTaskAmplitude.ToString(), nowTaskWidth.ToString(), bias.ToString(), (taskFinishTime-taskStartTime).ToString(), We.ToString(), AeOld.ToString(), Ae.ToString(), clear.ToString()};
        string s2 = string.Join(",", s1);
        if(swMT!=null)
        {
            swMT.WriteLine(s2);
        }
        if(clear==1 || clear == 0)
        {
            VariableManager.MTSum += taskFinishTime-taskStartTime;
            if(bias == Bias.Neutral)
            {
                VariableManager.AllMTSumNeutral += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Fast)
            {
                VariableManager.AllMTSumFast += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Accurate)
            {
                VariableManager.AllMTSumAccurate += taskFinishTime-taskStartTime;
            }
        }
        
    }
}