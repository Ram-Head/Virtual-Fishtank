using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO.Ports;
using UnityEngine.Networking;

public class ControlBoxes : MonoBehaviour
{
    public GameObject Control;
    public GameObject Box;
    public GameObject fish;
    public GameObject fishBlue;
    public GameObject screen;
    public int numberOfBoxes;
    List<FishBox> boxes = new List<FishBox>();
    List<GameObject> screens = new List<GameObject>();
    private float boxHeight = 8.2f;
    private int maxFishPerBox = 5;
    private Vector3 farthestBox = Vector3.zero;
    private RenderTexture rt;
    private Queue<ConnectionInstruction> newConnections = new Queue<ConnectionInstruction>();
    private Queue<ConnectionInstruction> newDisconnections = new Queue<ConnectionInstruction>();

    String host = "localhost";
    int port = 5000;

    string[] stringDelimiters = new string[] { ":", "~" }; //Items we want to ignore in strings
    char connect = '+';
    char disconnect = '-';
    SerialPort sp;

    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
    }

    private ConnectionInstruction makeInst(int boxno1, string wall1, string corner1,
        int boxno2, string wall2, string corner2)
    {
        BoxInfo box1 = new BoxInfo();
        box1.boxNo = boxno1;
        box1.corner = corner1;
        box1.wall = wall1;
        BoxInfo box2 = new BoxInfo();
        box2.boxNo = boxno2;
        box2.corner = corner2;
        box2.wall = wall2;
        ConnectionInstruction inst = new ConnectionInstruction();
        inst.timeStamp = -1;
        inst.firstBox = box1;
        inst.secondBox = box2;

        return inst;
    }

    // Use this for initialization
    void Start()
    {

        SpawnBoxes();
        Debug.Log("displays connected: " + Display.displays.Length);
        // Display.displays[0] is the primary, default display and is always ON.
        // Check if additional displays are available and activate each.
        if (Display.displays.Length > 2)
        {
            Display.displays[0].Activate();
            for (int i = 1; i < numberOfBoxes+1; i++)
            {
                if (Display.displays.Length > i)
                {
                    Display.displays[i].Activate();
                }
                Camera screenCam = boxes[i].screen.transform.Find("ScreenCamera").GetComponent<Camera>();
                screenCam.targetDisplay = i;

            }
        }
    }

    Coroutine avoidWifiCoroutine = null;
    // Update is called once per frame
    void Update()
    {
        //Debug.Log("pos: " + avoidPositionCoroutine);
        //Debug.Log(avoidWifiCoroutine);
        if (avoidWifiCoroutine == null /*(&& avoidPositionCoroutine == null*/)
        {
            //Debug.Log("A");
            avoidWifiCoroutine = StartCoroutine(getPositionUpdates());
        }

        ConnectionInstruction inst1 = makeInst(1, "bottom", "B", 0, "top", "B");
        ConnectionInstruction inst2 = makeInst(1, "bottom", "C", 0, "top", "A");
        ConnectionInstruction inst3 = makeInst(1, "bottom", "C", 0, "top", "A");
        ConnectionInstruction inst4 = makeInst(1, "bottom", "B", 0, "top", "B");
        ConnectionInstruction inst5 = makeInst(1, "bottom", "A", 0, "top", "B");
        ConnectionInstruction inst6 = makeInst(1, "bottom", "C", 0, "top", "D");
        ConnectionInstruction inst7 = makeInst(1, "bottom", "C", 0, "top", "D");
        //remember to set a coroutine block
        //
        //avoidPositionCoroutine = StartCoroutine(UpdatePosition());

        if (Input.GetKeyDown("1")) {
            /*foreach (Instruction inst in instructions)
            {
                //run disconnections first to avoid confusion about positions
                //if (!inst.connect) { inst.firstBox.breakConnections(inst.secondBox, inst.relationship); }
            }*/

            //foreach (Instruction inst in instructions)
            //{
            FishBox firstBox = boxes[inst1.firstBox.boxNo];
            FishBox secondBox = boxes[inst1.secondBox.boxNo];
            firstBox.createConnection(inst1, secondBox);
        }
        else if (Input.GetKeyDown("2"))
        {
            FishBox firstBox = boxes[inst2.firstBox.boxNo];
            FishBox secondBox = boxes[inst2.secondBox.boxNo];
            firstBox.createConnection(inst2, secondBox);
        }
        else if (Input.GetKeyDown("3"))
        {
            FishBox firstBox = boxes[inst3.firstBox.boxNo];
            FishBox secondBox = boxes[inst3.secondBox.boxNo];
            firstBox.breakConnection(inst3, secondBox);
        }
        else if (Input.GetKeyDown("4"))
        {
            FishBox firstBox = boxes[inst4.firstBox.boxNo];
            FishBox secondBox = boxes[inst4.secondBox.boxNo];
            firstBox.breakConnection(inst4, secondBox);
        }

        /*GameObject wall = boxes[0].getTankComponent("bottomWall");
        Renderer renderer = wall.transform.Find("A").GetComponent<Renderer>();
        Material mat = renderer.material;

        float emission = 0 + Mathf.PingPong(Time.time * (0.003f), .02f);
        Color baseColor = new Color(26, 55, 50); //Replace this with whatever you want for your base color at emission level '1'

        Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);

        mat.SetColor("_EmissionColor", finalColor);*/

    }


    /*IEnumerator lighstuff(){
        while (lightAnim)
        {
            GameObject wall = boxes[0].getTankComponent("bottomWall");
            Renderer renderer = wall.transform.Find("A").GetComponent<Renderer>();
            Material mat = renderer.material;

            if (mat.GetColor != goalColor)
            {
                float emission = Mathf.PingPong(Time.time * (0.3f), .5f);
                Color baseColor = new Color(44, 100, 95); //Replace this with whatever you want for your base color at emission level '1'

                Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);

                mat.SetColor("_EmissionColor", finalColor);
            }
            yield return null;
        }
    }*/


    Coroutine avoidPositionCoroutine = null;
    IEnumerator getDisconnectionUpdates()
    {
        UnityWebRequest www = UnityWebRequest.Get("http://localhost:5000/fishtank/disconnections");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string result = www.downloadHandler.text;
            while (result != "empty")
            {
                Debug.Log(result);
                ConnectionInstruction info = getConnectionInfo(result);
                newDisconnections.Enqueue(info);

                www = UnityWebRequest.Get("http://localhost:5000/fishtank/disconnections");
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                    break;
                }
                result = www.downloadHandler.text;
            }

        }
        avoidWifiCoroutine = null;
        //Debug.Log(newConnections.Count);
        if ((newDisconnections.Count > 0 || newConnections.Count > 0) /*&& avoidPositionCoroutine == null*/)
        {
            //Debug.Log("you");
            avoidPositionCoroutine = StartCoroutine(UpdatePosition());
        }
    }

        IEnumerator getPositionUpdates()
    {
        //Debug.Log("getText");
        UnityWebRequest www = UnityWebRequest.Get("http://localhost:5000/fishtank/connections");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string result = www.downloadHandler.text;
            while (result != "empty")
            {
                Debug.Log("!");
                Debug.Log(result);
                ConnectionInstruction info = getConnectionInfo(result);
                newConnections.Enqueue(info);

                www = UnityWebRequest.Get("http://localhost:5000/fishtank/connections");
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                    break;
                }
                result = www.downloadHandler.text;
            }
        }
        StartCoroutine(getDisconnectionUpdates());
    }

    IEnumerator UpdatePosition() {
        while (newDisconnections.Count != 0)
        {
            ConnectionInstruction inst = newDisconnections.Dequeue();
            FishBox firstBox = boxes[inst.firstBox.boxNo];
            FishBox secondBox = boxes[inst.secondBox.boxNo];
            firstBox.breakConnection(inst, secondBox);
        }
        while (newConnections.Count != 0) {
            ConnectionInstruction inst = newConnections.Dequeue();
            FishBox firstBox = boxes[inst.firstBox.boxNo];
            FishBox secondBox = boxes[inst.secondBox.boxNo];
            firstBox.createConnection(inst, secondBox);
        }
        Debug.Log("hey");
        avoidPositionCoroutine = null;
        yield return null;
    }

    [System.Serializable]
    public class BoxInfo
    {
        public int boxNo;
        public string corner;
        public string wall;

        public bool compareBoxInfo(BoxInfo box) {
            if (box.boxNo != boxNo || box.corner != corner || box.wall != wall) {
                return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class ConnectionInstruction
    {
        public int timeStamp;
        public BoxInfo firstBox;
        public BoxInfo secondBox;

        public bool sameInstruction(ConnectionInstruction inst) {
            if (!firstBox.compareBoxInfo(inst.firstBox)) {
                return false;
            }
            if (!secondBox.compareBoxInfo(inst.secondBox))
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return firstBox.boxNo + " " + firstBox.wall + " " + firstBox.corner + " + " +
                secondBox.boxNo + " " + secondBox.wall + " " + secondBox.corner;
        }
    }

    public static ConnectionInstruction getConnectionInfo(string json)
    {
        ConnectionInstruction info = JsonUtility.FromJson<ConnectionInstruction>(json);
        return info;
    }

    private void SpawnBoxes() {
        fish.SetActive(false);
        for (int i = 0; i < numberOfBoxes; i++)
        {
            Vector3 position = getEmptyPosition();
            FishBox currBox;
            if (i == 1)
            {
                currBox = new FishBox(maxFishPerBox, position, Box, Control, fishBlue, i, screen);
            }
            else {
                currBox = new FishBox(maxFishPerBox, position, Box, Control, fish, i, screen);
            }
            boxes.Add(currBox);
            //GameObject currScreen;
            //currScreen = SpawnScreen(position + screen.transform.position, screen, currBox);
            //screens.Add(currScreen);
        }

        Box.SetActive(false);
        screen.SetActive(false);
    }

    //i kind of cheated in making this because I wasn't sure how to ensure an empty position
    public Vector3 getEmptyPosition() {
        Vector3 emptyPos = farthestBox; 
        int offset = 20;
        farthestBox.x += offset;
        farthestBox.z += offset;
        return emptyPos;
    }

    private class FishBox
    {
        private HashSet<GameObject> fishes = new HashSet<GameObject>();
        public int boxNumber;
        public GameObject fishTank;
        private float boxHeight = 8.2f;
        private float boxWidthX = 8.2f;
        private float boxWidthZ = 8.2f;
        private Dictionary<FishBox, List<ConnectionInstruction>> connectedBoxes = new Dictionary<FishBox, List<ConnectionInstruction>>();
        public GameObject screen;
        private RenderTexture lefRT;
        private RenderTexture rightRT;
        private RenderTexture frontRT;

        private String A = "A";
        private String B = "B";
        private String C = "C";
        private String D = "D";

        public FishBox(int maxFish, Vector3 position, GameObject prefabBox, 
            GameObject parent, GameObject prefabFish, int boxNo, GameObject prefabScreen)
        {
            fishTank = Instantiate(prefabBox, position, prefabBox.transform.rotation, parent.transform);
            boxNumber = boxNo;
            for (int j = 0; j < UnityEngine.Random.Range(1, maxFish); j++)
            {
                GameObject currFish;
                currFish = Instantiate(prefabFish, fishTank.transform.position, fishTank.transform.rotation, fishTank.transform);
                currFish.SetActive(true);

                float scale = UnityEngine.Random.Range(0f, 1f);
                currFish.transform.localScale += new Vector3(scale, scale, scale);

                fishes.Add(currFish);
            }
            screen = SpawnScreen(position + prefabScreen.transform.position, prefabScreen);
        }

        private void setUpScreen(String name, GameObject currScreen, RenderTexture rt) {
            
            rt = new RenderTexture(500, 500, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            Camera frontcam = fishTank.transform.Find(name + "Camera").GetComponent<Camera>();
            frontcam.targetTexture = rt;
            GameObject frontScreen = currScreen.transform.Find("ScreenGroup").gameObject;
            frontScreen = frontScreen.transform.Find(name + "View").gameObject;
            Renderer screenRenderer = frontScreen.GetComponent<Renderer>();
            screenRenderer.material.SetTexture("_MainTex", rt);
            
        }

        private GameObject SpawnScreen(Vector3 position, GameObject prefabScreen)
        {
            GameObject currScreen = Instantiate(prefabScreen, position, prefabScreen.transform.rotation, prefabScreen.transform.parent.transform);
            setUpScreen("Front", currScreen, frontRT);
            setUpScreen("Left", currScreen, lefRT);
            setUpScreen("Right", currScreen, rightRT);
            return currScreen;
        }

        public void createConnection(ConnectionInstruction inst, FishBox otherBox)
        {
            Debug.Log(inst.ToString());
            BoxInfo thisBoxInst = inst.firstBox;
            BoxInfo otherBoxInst = inst.secondBox;
            string thisCorner = thisBoxInst.corner;
            string thisWall = thisBoxInst.wall;
            string otherCorner = otherBoxInst.corner;
            string otherWall = otherBoxInst.wall;
            if (connectedBoxes.ContainsKey(otherBox))
            {
                foreach (ConnectionInstruction i in connectedBoxes[otherBox])
                {
                    if (inst.sameInstruction(i))
                    {
                        Debug.Log("Cannot complete instruction " + inst.ToString() + " because the connection already exists");
                        return;
                    }
                }
                addConnection(inst, otherBox);
            }
            else if (this == otherBox)
            {
                Debug.Log("Cannot complete instruction " + inst.ToString() + ". Cant connect box to itself");
            }
            else
            {

                Vector3 otherPos = otherBox.getPosition();
                //apply to all connections - write a helper method would you
                int direction = 1;
                if (otherBoxInst.wall == "bottom") { direction = -1; }
                //so we need to do movements relative otherbox
                fishTank.transform.position = otherPos + (otherBox.fishTank.transform.up * boxWidthZ * direction);
                if (otherCorner == "A" || otherCorner == "B")
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.forward * (boxWidthZ / 2));
                }
                else
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.forward * (-boxWidthZ / 2));
                }

                if (otherCorner == "B" || otherCorner == "D")
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.right * (boxWidthX / 2));
                }
                else
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.right * (-boxWidthX / 2));
                }

                makeCornersTouch(thisBoxInst, otherBoxInst, otherBox, fishTank.transform.position);

                setWallCorner(false, thisWall, thisCorner);
                otherBox.setWallCorner(false, otherWall, otherCorner);

                connectedBoxes[otherBox] = new List<ConnectionInstruction> { inst };
                otherBox.connectedBoxes[this] = new List<ConnectionInstruction> { inst };
            }

        }



        /*public void createConnection(ConnectionInstruction inst, FishBox otherBox) {
            Debug.Log(inst.ToString());
            BoxInfo thisBoxInst = inst.firstBox;
            BoxInfo otherBoxInst = inst.secondBox;
            string thisCorner = thisBoxInst.corner;
            string thisWall = thisBoxInst.wall;
            string otherCorner = otherBoxInst.corner;
            string otherWall = otherBoxInst.wall;
            if (connectedBoxes.ContainsKey(otherBox))
            {
                foreach (ConnectionInstruction i in connectedBoxes[otherBox])
                {
                    if (inst.sameInstruction(i))
                    {
                        Debug.Log("Cannot complete instruction " + inst.ToString() + " because the connection already exists");
                        return;
                    }
                }
                addConnection(inst, otherBox);
            }
            else if (this == otherBox) {
                Debug.Log("Cannot complete instruction "+inst.ToString()+". Cant connect box to itself");
            }
            else
            { 
    
                Vector3 otherPos = otherBox.getPosition();
                //apply to all connections - write a helper method would you
                int direction = 1;
                if (otherBoxInst.wall == "bottom") { direction = -1; }
                //so we need to do movements relative otherbox
                fishTank.transform.position = otherPos + (otherBox.fishTank.transform.up * boxWidthZ*direction);
                if (otherCorner == "A" || otherCorner == "B")
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.forward * (boxWidthZ / 2));
                }
                else
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.forward * (-boxWidthZ / 2));
                }

                if (otherCorner == "B" || otherCorner == "D")
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.right * (boxWidthX / 2));
                }
                else
                {
                    fishTank.transform.position += (otherBox.fishTank.transform.right * (-boxWidthX / 2));
                }

                makeCornersTouch(thisBoxInst, otherBoxInst, otherBox, fishTank.transform.position);

                setWallCorner(false, thisWall, thisCorner);
                otherBox.setWallCorner(false, otherWall, otherCorner);

                connectedBoxes[otherBox] = new List<ConnectionInstruction> {inst};
                otherBox.connectedBoxes[this] = new List<ConnectionInstruction> { inst };
            }
        }*/

        private void makeCornersTouch(BoxInfo thisBoxInst, BoxInfo otherBoxInst, FishBox otherBox, Vector3 pivot) {
            string thisCorner = thisBoxInst.corner;
            string otherCorner = otherBoxInst.corner;
            Quaternion otherRot = otherBox.getRotation();
            fishTank.transform.rotation = otherRot;
            String newThisCorner = thisCorner;
            int rotSign = 1;
            if (thisBoxInst.wall == otherBoxInst.wall)
            {
                //I would also write method for this, it needs to flip the fish to work right
                fishTank.transform.rotation *= Quaternion.Euler(180 * Vector3.forward);
                if (thisCorner == "A") { newThisCorner = "B"; }
                else if (thisCorner == "B") { newThisCorner = "A"; }
                else if (thisCorner == "C") { newThisCorner = "D"; }
                else if (thisCorner == "D") { newThisCorner = "C"; }
                rotSign = -1;
            }
            Debug.Log(pivot);
            List<String> corners = new List<String>(new String[] { "D", "C", "A", "B" });
            int offset = ((2 - corners.IndexOf(newThisCorner) + corners.IndexOf(otherCorner)) * rotSign * 90);
            fishTank.transform.RotateAround(pivot, Vector3.up, offset);
        }

        private void addConnection(ConnectionInstruction inst, FishBox otherBox)
        {
            BoxInfo thisBoxInst = inst.firstBox;
            BoxInfo otherBoxInst = inst.secondBox;
            string thisCorner = thisBoxInst.corner;
            string thisWall = thisBoxInst.wall;
            string otherCorner = otherBoxInst.corner;
            string otherWall = otherBoxInst.wall;
            if (connectedBoxes[otherBox][0].secondBox.wall != otherWall ||
                    connectedBoxes[otherBox][0].firstBox.wall != thisWall)
            {
                Debug.Log("Cannot complete instruction " + inst.ToString() +
                    ". A box cannot be connected to two different walls on the same box");
                return;
            }
            if (connectedBoxes[otherBox].Count >= 2)
            {
                connectedBoxes[otherBox].Add(inst);
                otherBox.connectedBoxes[this].Add(inst);
                return;
            }
            else
            {
                Vector3 otherPos = otherBox.getPosition();
                //apply to all connections - write a helper method would you
                int direction = 1;
                if (otherBoxInst.wall == "bottom") { direction = -1; }
                //so we need to do movements relative otherbox
                fishTank.transform.rotation = otherBox.fishTank.transform.rotation;
                fishTank.transform.position = otherPos + (otherBox.fishTank.transform.up * boxWidthZ * direction);
                connectWalls(thisWall, otherBox, otherWall, true);

                connectedBoxes[otherBox].Add(inst);
                otherBox.connectedBoxes[this].Add(inst);
                return;
            }
        }
            /*private void addConnection(ConnectionInstruction inst, FishBox otherBox) {
                BoxInfo thisBoxInst = inst.firstBox;
                BoxInfo otherBoxInst = inst.secondBox;
                string thisCorner = thisBoxInst.corner;
                string thisWall = thisBoxInst.wall;
                string otherCorner = otherBoxInst.corner;
                string otherWall = otherBoxInst.wall;

                if (connectedBoxes[otherBox][0].secondBox.wall != otherWall ||
                    connectedBoxes[otherBox][0].firstBox.wall != thisWall) {
                    Debug.Log("Cannot complete instruction " + inst.ToString() +
                        ". A box cannot be connected to two different walls on the same box");
                    return;
                }
                if (connectedBoxes[otherBox].Count > 2)
                {
                    connectedBoxes[otherBox].Add(inst);
                    otherBox.connectedBoxes[this].Add(inst);
                    return;
                }
                else if (!getTankComponent(thisWall + "Wall").activeSelf &&
                    !otherBox.getTankComponent(otherWall + "Wall").activeSelf)
                {
                    connectedBoxes[otherBox].Add(inst);
                    otherBox.connectedBoxes[this].Add(inst);
                    return;
                }
                else if (connectedBoxes[otherBox].Count == 2)
                {
                    //if already 2 connections check for if it could touch and set wall, else do nothing
                    connectedBoxes[otherBox].Add(inst);
                    otherBox.connectedBoxes[this].Add(inst);
                }
                else {
                    List<string> thisAdj = getAdjacentCorners(thisCorner);
                    List<string> otherAdj = getAdjacentCorners(otherCorner);
                    string thisDia = getDiagonalCorner(thisCorner);
                    string otherDia = getDiagonalCorner(otherCorner);

                    BoxInfo thisPrevInst = connectedBoxes[otherBox][0].firstBox;
                    BoxInfo otherPrevInst = connectedBoxes[otherBox][0].secondBox;
                    if (otherPrevInst.boxNo == thisBoxInst.boxNo) {
                        thisPrevInst = connectedBoxes[otherBox][0].secondBox;
                        otherPrevInst = connectedBoxes[otherBox][0].firstBox;
                    }
                    //dia dia
                    if (thisDia == thisPrevInst.corner
                        && otherDia == otherPrevInst.corner) {
                        fishTank.transform.rotation = (fishTank.transform.rotation * Quaternion.Euler(Vector3.up * 180));
                        Vector3 newPos = otherBox.getPosition();
                        newPos.y = getPosition().y;
                        fishTank.transform.position = newPos;
                        connectCorners(inst, otherBox, false);
                        connectWalls(thisWall, otherBox, otherWall, true);
                    }
                    //adj adj
                    else if (thisAdj.Contains(thisPrevInst.corner)
                        && otherAdj.Contains(otherPrevInst.corner))
                    {

                        GameObject wall = otherBox.getTankComponent(otherPrevInst.wall + "Wall");
                        GameObject corner = wall.transform.Find(otherPrevInst.corner).gameObject;
                        Debug.Log(corner.transform.position);
                        fishTank.transform.RotateAround(corner.transform.position, Vector3.up, 90);
                        //makeCornersTouch(getThisBoxInst(inst), otherPrevInst, otherBox, corner.transform.position);

                        //fishTank.transform
                        //rotate so newthis corner is on prevothercorner
                        //slide
                    }
                    else {
                        Debug.Log("not implemented");
                    }

                    //adj adj
                    //dia adj
                    //adj dia

                    //rotate

                }
                /*check wall status - if off do nothing
                 * if already 3 connections - do nothing
                 * 
                 * if already 2 connections check for if it could touch and set wall, else do nothing
                 * if 1 connection figure out how to rotate, if diagonal set wall

                 */
            // if were at 2 already then you can rotate anymore, so if were on a good side then go full wall
            //if 3+ or diag then go for full wall,
        //}*/
        private void connectCorners(ConnectionInstruction inst, FishBox otherBox, bool animation)
        {
            connectedBoxes[otherBox].Add(inst);
            otherBox.connectedBoxes[this].Add(inst);
            //animation
        }

        private void connectWalls(string thisWall, FishBox otherBox, string otherWall, bool animation)
        {
            setTankWall(thisWall, false);
            otherBox.setTankWall(otherWall, false);
            //animation
        }

        private List<string> getAdjacentCorners(string corner) {
            if (corner == "A") { return new List<string>{ "D","B"}; }
            if (corner == "D") { return new List<string> { "C", "A" }; }
            if (corner == "B") { return new List<string> { "A", "C" }; }
            if (corner == "C") { return new List<string> { "B", "D" }; }
            return null;
        }

        private string getDiagonalCorner(string corner)
        {
            if (corner == "A") { return "D"; }
            if (corner == "D") { return "A"; }
            if (corner == "B") { return "C"; }
            if (corner == "C") { return "B"; }
            return null;
        }

        public Vector3 getPosition() {
            return fishTank.transform.position;
        }

        public Quaternion getRotation()
        {
            return fishTank.transform.rotation;
        }

        public void setPosition(Vector3 newPosition)
        {
            fishTank.transform.position = newPosition;
        }

        private bool outsideRange(float item, float center, float width) {
            if (item < center - (width / 2)) { return true; }
            if (item > center + (width / 2)) { return true; }
            return false;
        }

        private bool tankContains(GameObject fish) {
            Vector3 fishPosition = fish.transform.position;
            Vector3 tankPosition = getPosition();
            if (outsideRange(fishPosition.x, tankPosition.x, boxWidthX)) {
                return false;
            }
            if (outsideRange(fishPosition.y, tankPosition.y, boxHeight))
            {
                return false;
            }
            if (outsideRange(fishPosition.z, tankPosition.z, boxWidthZ))
            {
                return false;
            }
            return true;
        }

        public void breakConnection(ConnectionInstruction instruction, FishBox otherBox) {
            string otherCorner = instruction.secondBox.corner;
            string otherWall = instruction.secondBox.wall;
            List<ConnectionInstruction> sharedConnections = new List<ConnectionInstruction>();
            if (connectedBoxes.ContainsKey(otherBox))
            {
                sharedConnections = connectedBoxes[otherBox];
            }
            Queue<ConnectionInstruction> keepConnections = new Queue<ConnectionInstruction>();
            ConnectionInstruction trashInstruction = null;

            foreach (ConnectionInstruction inst in sharedConnections) {
                if (inst.sameInstruction(instruction))
                {
                    Debug.Log("AA");
                    trashInstruction = inst;
                }
                else {
                    keepConnections.Enqueue(inst);
                }
            }

            if (trashInstruction != null) {
                if (keepConnections.Count >= 2 /*||is diagonal connection*/)
                {
                    connectedBoxes[otherBox].Remove(trashInstruction);
                    otherBox.connectedBoxes[this].Remove(trashInstruction);
                }
                else
                {
                    separateFrom(otherBox, true);
                    while (keepConnections.Count != 0)
                    {
                        ConnectionInstruction inst = keepConnections.Dequeue();
                        createConnection(inst, otherBox);
                    }
                }
            }
            else {
                Debug.Log("Could not break connection " + instruction + " because the boxes dont share this connection");
            }
        }

        private BoxInfo getThisBoxInst(ConnectionInstruction inst) {
            BoxInfo thisBoxInst = inst.firstBox;
            if (thisBoxInst.boxNo != boxNumber)
            {
                thisBoxInst = inst.secondBox;
            }
            return thisBoxInst;
        }

        public void separateFrom(FishBox otherBox, bool moveAway) {
            if (connectedBoxes.ContainsKey(otherBox))
            {
                setTankWall(getThisBoxInst(connectedBoxes[otherBox][0]).wall, true);
                foreach (ConnectionInstruction inst in connectedBoxes[otherBox])
                {
                    BoxInfo thisBoxInst = getThisBoxInst(inst);
                    setWallCorner(true, thisBoxInst.wall, thisBoxInst.corner);
                }
                connectedBoxes.Remove(otherBox);
                HashSet<GameObject> newFish = new HashSet<GameObject>();
                foreach (GameObject fish in otherBox.getFishes())
                {
                    if (tankContains(fish))
                    {
                        newFish.Add(fish);
                        //Debug.Log("fish!");
                    }
                }
                foreach (GameObject fish in newFish)
                {
                    fish.transform.parent = fishTank.transform;
                    otherBox.getFishes().Remove(fish);
                    fishes.Add(fish);
                }
                otherBox.separateFrom(this, false);
                if (moveAway) {
                    Vector3 newPos = Vector3.zero;
                    newPos.x += 20;
                    newPos.z += 20;
                    fishTank.transform.position = newPos;
                    fishTank.transform.rotation = Quaternion.Euler(Vector3.zero);
                }
            }
        }

        public HashSet<GameObject> getFishes() {
            return fishes;
        }

        public GameObject getTankComponent(string name) {
            try
            {
                GameObject tank = fishTank.transform.Find("basictank").gameObject;
                return tank.transform.Find(name).gameObject;
            }
            catch {
                Debug.Log("Failed to get tank component " + name);
                return null;
            }
        }
        private void setTankWall(string name, bool active)
        {
            Debug.Log("A");
            try
            {
                GameObject comp = getTankComponent(name + "Wall");
                comp.SetActive(active);
            }
            catch (System.Exception)
            {
                Debug.Log("Failed to access wall " + name);
            }
        }

        private void setWallCorner(bool active, string wallName, string cornerName) {
            try
            {
                GameObject wall = getTankComponent(wallName + "Wall");
                GameObject corner = wall.transform.Find(cornerName).gameObject;
                corner.SetActive(active);
            }
            catch
            {
                Debug.Log("Failed to access wall " + wallName + "Wall" +", corner " + cornerName);
            }

        }

    }


    void RotateObject(Quaternion degree)
    //void RotateObject(Vector3 degree)
    {
        //target.transform.rotation = degree;

        //target.transform.rotation = Quaternion.Slerp(target.transform.rotation, Quaternion.Euler(degree), Time.deltaTime * 2f);
    }
}

