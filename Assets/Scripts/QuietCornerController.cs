using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QuietCornerController : MonoBehaviour
{
    public Camera viewCamera;
    public CharacterController player;
    public Transform orb;
    public Light sun, orbLight;
    public Light[] lamps;
    public Renderer[] lampSurfaces;
    public TextMesh lightStatus, soundStatus, pauseStatus, orbStatus;
    public float walkingSpeed = 1.6f;
    public bool warm = true, soundOn, guideRunning;
    public int completedCycles;
    public float guideTime;

    private AudioSource ambience;
    private float yaw, pitch, moodBlend = 1f;
    private float baseOrbSize = .72f;
    private Material orbMaterial;
    private string lastPhase = "";
    private bool verifying;
    private int errorCount;
    private readonly List<string> checks = new List<string>();

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        ambience.spatialBlend = 0f;
        ambience.volume = 0f;
        ambience.clip = MakeAmbientClip();
        orbMaterial = orb.GetComponent<Renderer>().material;
        Application.logMessageReceived += CountErrors;
        ResetExperience();
#if !UNITY_WEBGL || UNITY_EDITOR
        string[] args = Environment.GetCommandLineArgs();
        verifying = Array.IndexOf(args, "-qc-verify") >= 0;
        if (verifying) StartCoroutine(VerifyAndCapture(args));
#endif
    }

    void OnDestroy() { Application.logMessageReceived -= CountErrors; }
    void CountErrors(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorCount++;
    }

    // Original, softly filtered procedural water-like sound; no downloaded audio assets.
    AudioClip MakeAmbientClip()
    {
        const int rate = 22050;
        const int seconds = 12;
        float[] samples = new float[rate * seconds];
        var rng = new System.Random(9902);
        float filtered = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            filtered = .965f * filtered + .035f * ((float)rng.NextDouble() * 2 - 1);
            float t = (float)i / rate;
            float fade = Mathf.Min(1f, Mathf.Min(t, seconds - t) / .7f);
            samples[i] = filtered * (.35f + .1f * Mathf.Sin(t * Mathf.PI / 3)) * fade;
        }
        var clip = AudioClip.Create("Original gentle water texture", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        if (!verifying) ReadInput();
        moodBlend = Mathf.MoveTowards(moodBlend, warm ? 1f : 0f, Time.deltaTime * .65f);
        ApplyMood();
        ambience.volume = Mathf.MoveTowards(ambience.volume, soundOn ? .55f : 0f, Time.deltaTime * .4f);
        if (!soundOn && ambience.volume <= .001f && ambience.isPlaying) ambience.Stop();
        if (guideRunning)
        {
            guideTime += Time.deltaTime;
            completedCycles = Mathf.FloorToInt(guideTime / 8f);
            if (guideTime >= 48f)
            {
                guideRunning = false;
                guideTime = 48f;
                completedCycles = 6;
                SetPhase("PAUSE COMPLETE\nStay as long as you like");
                pauseStatus.text = "START PAUSE\n48-second visual guide";
            }
            else
            {
                float phase = guideTime % 8f;
                float size = .72f + .28f * (.5f - .5f * Mathf.Cos(phase / 8f * Mathf.PI * 2));
                orb.localScale = Vector3.one * size;
                SetPhase((phase < 4f ? "GENTLY EXPAND" : "GENTLY SETTLE") + "\n" + (completedCycles + 1) + " / 6");
            }
        }
        else orb.localScale = Vector3.Lerp(orb.localScale, Vector3.one * baseOrbSize, Time.deltaTime * 3f);
    }

    void SetPhase(string text)
    {
        if (text == lastPhase) return;
        orbStatus.text = text;
        lastPhase = text;
    }

    void ApplyMood()
    {
        Color glow = Color.Lerp(new Color(.43f,.8f,.9f), new Color(1f,.69f,.34f), moodBlend);
        sun.color = Color.Lerp(new Color(.75f,.88f,1f), new Color(1f,.83f,.65f), moodBlend);
        sun.intensity = Mathf.Lerp(.85f, 1.15f, moodBlend);
        RenderSettings.ambientLight = Color.Lerp(new Color(.37f,.47f,.53f),new Color(.52f,.47f,.38f),moodBlend);
        viewCamera.backgroundColor = Color.Lerp(new Color(.55f,.72f,.8f),new Color(.84f,.75f,.61f),moodBlend);
        foreach (Light lamp in lamps) { lamp.color = glow; lamp.intensity = .85f; }
        foreach (Renderer surface in lampSurfaces)
        {
            surface.material.color = glow;
            surface.material.SetColor("_EmissionColor",glow*.7f);
        }
        orbMaterial.color = glow;
        orbMaterial.SetColor("_EmissionColor",glow*.5f);
        orbLight.color = glow;
    }

    void ReadInput()
    {
        // Looking is always available, including during the visual guide.
        bool looking = Input.GetMouseButton(1);
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
        if (looking)
        {
            yaw += Input.GetAxisRaw("Mouse X") * 2f;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2f, -70, 70);
        }
        if (Input.GetKeyDown(KeyCode.Q)) yaw -= 30f;
        if (Input.GetKeyDown(KeyCode.E)) yaw += 30f;
        player.transform.rotation = Quaternion.Euler(0,yaw,0);
        viewCamera.transform.localRotation = Quaternion.Euler(pitch,0,0);
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction += player.transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction -= player.transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction += player.transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction -= player.transform.right;
        player.Move(direction.normalized * walkingSpeed * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = looking ? viewCamera.ViewportPointToRay(new Vector3(.5f,.5f,0)) : viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 25f))
            {
                var target = hit.collider.GetComponentInParent<QuietCornerTarget>();
                if (target != null) Activate(target.action,target.destination);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) Activate("light",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Activate("sound",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Activate("pause",Vector3.zero);
        if (Input.GetKeyDown(KeyCode.R)) ResetExperience();
        if (Input.GetKeyDown(KeyCode.Home)) Teleport(new Vector3(0,.05f,-6.3f));
        if (Input.GetKeyDown(KeyCode.Escape)) StopExperience();
    }

    public void Activate(string action, Vector3 destination)
    {
        switch (action)
        {
            case "light": warm = !warm; lightStatus.text = (warm ? "WARM LIGHT" : "COOL LIGHT") + "\nClick to switch"; break;
            case "sound": soundOn = !soundOn; if(soundOn && !ambience.isPlaying) ambience.Play(); soundStatus.text = (soundOn ? "SOUND ON" : "SOUND OFF") + "\nClick to toggle"; break;
            case "pause":
                if(guideRunning) StopGuide();
                else { guideRunning=true; guideTime=0; completedCycles=0; pauseStatus.text="STOP PAUSE\nYou can stop at any time"; }
                break;
            case "stop": StopExperience(); break;
            case "reset": ResetExperience(); break;
            case "teleport": Teleport(destination); break;
        }
    }

    public void Teleport(Vector3 destination)
    {
        player.enabled=false;
        player.transform.position=destination;
        player.enabled=true;
    }

    void StopGuide()
    {
        guideRunning=false;
        guideTime=0;
        completedCycles=0;
        SetPhase("YOUR PACE\nNo need to match the orb");
        pauseStatus.text="START PAUSE\n48-second visual guide";
    }

    public void StopExperience()
    {
        StopGuide(); soundOn=false; ambience.Stop(); ambience.volume=0;
        soundStatus.text="SOUND OFF\nClick to toggle";
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
    }

    public void ResetExperience()
    {
        StopExperience(); warm=true; lightStatus.text="WARM LIGHT\nClick to switch";
        // Reset never forces the user's position or view direction.
    }

    void OnApplicationFocus(bool focused) { if(!focused && !verifying) StopExperience(); }

    void Check(bool condition,string label)
    {
        checks.Add((condition ? "PASS: " : "FAIL: ")+label);
        if(!condition) Debug.LogError("Verification failed: "+label);
    }

    IEnumerator Capture(string path)
    {
        yield return new WaitForEndOfFrame();
        // Render the actual active scene even if Windows has occluded the test window.
        var texture = new RenderTexture(1600,900,24);
        var previous = RenderTexture.active;
        var previousTarget = viewCamera.targetTexture;
        viewCamera.targetTexture=texture;
        viewCamera.Render();
        RenderTexture.active=texture;
        var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,1600,900),0,0);
        pixels.Apply();
        File.WriteAllBytes(path,pixels.EncodeToPNG());
        viewCamera.targetTexture=previousTarget;
        RenderTexture.active=previous;
        Destroy(pixels);
        texture.Release();
        Destroy(texture);
    }

    IEnumerator VerifyAndCapture(string[] args)
    {
        int index=Array.IndexOf(args,"-qc-output");
        string output=index>=0 && index+1<args.Length ? args[index+1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        yield return new WaitForSeconds(2f);
        Check(warm && !soundOn && !guideRunning,"Safe initial state: warm, silent, guide stopped");
        Check(Physics.Raycast(viewCamera.transform.position,(lightStatus.transform.position-viewCamera.transform.position).normalized,out var rayHit,20f) && rayHit.collider.GetComponent<QuietCornerTarget>() != null,"Physical lighting button is reachable by pointer ray");
        yield return Capture(Path.Combine(output,"01_Warm_Overview.png"));
        Activate("light",Vector3.zero);
        Activate("sound",Vector3.zero);
        yield return new WaitForSeconds(2f);
        Check(!warm && soundOn && ambience.isPlaying && ambience.volume>.4f,"Lighting and audio toggles change scene and play sound");
        yield return Capture(Path.Combine(output,"02_Cool_Sound_On.png"));
        Activate("pause",Vector3.zero);
        yield return new WaitForSeconds(2f);
        Check(guideRunning && guideTime>1 && orb.localScale.x>.8f,"Pause guide animates orb and advances time");
        yield return Capture(Path.Combine(output,"03_Visual_Pause.png"));
        Activate("pause",Vector3.zero);
        Check(!guideRunning && guideTime==0,"Pause control cancels active guide");
        Activate("pause",Vector3.zero);
        guideTime=47.9f;
        yield return new WaitForSeconds(.3f);
        Check(!guideRunning && completedCycles==6,"Guide completes after six cycles");
        Activate("stop",Vector3.zero);
        Check(!guideRunning && !soundOn && !ambience.isPlaying,"Stop immediately silences sound and stops guide");
        Vector3 prior=player.transform.position;
        var targets=FindObjectsByType<QuietCornerTarget>(FindObjectsSortMode.None);
        var pad=Array.Find(targets,t=>t.action=="teleport");
        Activate("teleport",pad.destination);
        Check(Vector3.Distance(player.transform.position,pad.destination)<.05f,"Floor destination moves the player");
        Teleport(prior);
        player.Move(Vector3.forward*.5f);
        Check(Vector3.Distance(player.transform.position,prior)>.3f,"Character controller can move through the room");
        Teleport(prior);
        player.transform.rotation=Quaternion.Euler(0,20,0);
        Check(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,20))<.1f,"View can freely rotate");
        player.transform.rotation=Quaternion.identity;
        ResetExperience();
        Check(warm && !soundOn && !guideRunning,"Reset restores neutral interaction state");
        yield return new WaitForSeconds(2f);
        Check(errorCount==0,"No runtime errors during automated smoke check");
        File.WriteAllLines(Path.Combine(output,"verification.txt"),checks);
        Application.Quit(errorCount==0 ? 0 : 1);
    }
}
