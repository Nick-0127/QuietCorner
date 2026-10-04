using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class QuietCornerBuilder
{
    static Material wood,wall,dark,cream,teal,leaf,stone,glow;
    static Font font;
    static Transform room,details,controls;

    [MenuItem("Quiet Corner/Rebuild prototype scene")]
    public static void CreateScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        wood=Mat("Oak",new Color(.46f,.31f,.19f));
        wall=Mat("Limestone",new Color(.79f,.76f,.65f));
        dark=Mat("Ink",new Color(.065f,.13f,.14f));
        cream=Mat("Linen",new Color(.93f,.87f,.72f));
        teal=Mat("Sage",new Color(.22f,.43f,.38f));
        leaf=Mat("Foliage",new Color(.19f,.36f,.22f));
        stone=Mat("Stone",new Color(.43f,.49f,.44f));
        glow=Mat("Lantern",new Color(1,.72f,.35f),true);
        room=new GameObject("01 Architecture").transform;
        details=new GameObject("02 Furniture and landscape").transform;
        controls=new GameObject("03 Spatial controls").transform;
        Box("Floor",new Vector3(0,-.16f,-.5f),new Vector3(12,.3f,15),wood,room);
        for(int i=-7;i<=7;i++) Box("Floor seam",new Vector3(0,.001f,i),new Vector3(12,.004f,.012f),dark,room,false);
        Box("Left wall",new Vector3(-6,2,-.5f),new Vector3(.25f,4,15),wall,room);
        Box("Right wall",new Vector3(6,2,-.5f),new Vector3(.25f,4,15),wall,room);
        Box("Rear low wall",new Vector3(0,.38f,5),new Vector3(12,.76f,.3f),wall,room);
        Box("Rear header",new Vector3(0,4.55f,5),new Vector3(12,1.15f,.3f),wall,room);
        Box("Rear left",new Vector3(-5,2.4f,5),new Vector3(2,3.3f,.3f),wall,room);
        Box("Rear right",new Vector3(5,2.4f,5),new Vector3(2,3.3f,.3f),wall,room);
        Box("Window sill",new Vector3(0,.8f,4.85f),new Vector3(8.3f,.18f,.6f),wood,room);
        for(int i=-4;i<=4;i+=2) Box("Window mullion",new Vector3(i,2.4f,5),new Vector3(.075f,3.25f,.16f),wood,room);
        // Invisible boundaries keep the desktop prototype inside the modelled space.
        var rear=Box("Window safety boundary",new Vector3(0,2.3f,5.15f),new Vector3(12,4.6f,.1f),wall,room);
        rear.GetComponent<Renderer>().enabled=false;
        var front=Box("Entry boundary",new Vector3(0,1,-8),new Vector3(12,2,.1f),wall,room);
        front.GetComponent<Renderer>().enabled=false;
        for(int i=-5;i<=5;i++)
        {
            var beam=Box("Ceiling oak beam",new Vector3(i,4.85f,-.5f),new Vector3(.11f,.14f,15),wood,room);
            beam.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        Text("QUIET CORNER",new Vector3(0,4.35f,4.8f),.23f,dark.color,room);
        Text("A small space to choose your own pace",new Vector3(0,3.98f,4.78f),.075f,dark.color,room);
        // Outdoor scene, visible through the long window.
        Box("Garden",new Vector3(0,-.18f,15),new Vector3(70,.15f,25),teal,details,false);
        for(int i=0;i<7;i++)
        {
            var hill=Primitive(PrimitiveType.Sphere,"Distant hill",new Vector3((i-3)*7,1.2f,20+(i%2)*8),new Vector3(13,7+i%3,13),stone,details,false);
        }
        for(int i=0;i<9;i++) Tree(new Vector3((i-4)*3.4f,0,10+(i%3)*2));
        Primitive(PrimitiveType.Cylinder,"Round woven rug",new Vector3(0,.018f,.8f),new Vector3(6,.015f,6),teal,details,false);
        Primitive(PrimitiveType.Cylinder,"Rug inner",new Vector3(0,.035f,.8f),new Vector3(5.7f,.008f,5.7f),cream,details,false);
        Box("Bench base",new Vector3(0,.38f,3.3f),new Vector3(4.6f,.3f,1),wood,details);
        Box("Bench cushion",new Vector3(0,.6f,3.3f),new Vector3(4.45f,.2f,.92f),teal,details);
        Box("Bench back",new Vector3(0,1.06f,3.72f),new Vector3(4.6f,.8f,.16f),wood,details);
        for(int i=-1;i<=1;i+=2)
        {
            Box("Bench leg",new Vector3(i*1.75f,.2f,3.3f),new Vector3(.18f,.4f,.7f),wood,details);
            Box("Linen cushion",new Vector3(i*1.5f,.95f,3.38f),new Vector3(.65f,.5f,.22f),cream,details);
            Plant(new Vector3(i*4.8f,0,3.2f));
            Plant(new Vector3(i*5.2f,0,-2.8f));
        }
        Primitive(PrimitiveType.Cylinder,"Orb pedestal",new Vector3(0,.47f,1.1f),new Vector3(.85f,.47f,.85f),stone,details);
        var orb=Primitive(PrimitiveType.Sphere,"Visual guide orb",new Vector3(0,1.75f,1.1f),Vector3.one*.72f,glow,details,false);
        var orbLight=new GameObject("Orb glow").AddComponent<Light>();
        orbLight.transform.position=new Vector3(0,2,1.1f); orbLight.type=LightType.Point;orbLight.range=3;orbLight.intensity=.4f;
        orbLight.transform.parent=details;
        var orbStatus=Text("YOUR PACE\nNo need to match the orb",new Vector3(0,2.65f,1.1f),.072f,dark.color,controls);
        // Broad console: three large, physically separated spatial controls.
        Box("Console shelf",new Vector3(0,.97f,-1.05f),new Vector3(4.6f,.13f,.65f),wood,controls);
        Box("Console left leg",new Vector3(-2,.5f,-1.05f),new Vector3(.12f,1,.45f),wood,controls);
        Box("Console right leg",new Vector3(2,.5f,-1.05f),new Vector3(.12f,1,.45f),wood,controls);
        var lightText=Button("LIGHT",new Vector3(-1.5f,1.28f,-1.05f),"light","WARM LIGHT\nClick to switch",1.35f,.63f);
        var soundText=Button("SOUND",new Vector3(0,1.28f,-1.05f),"sound","SOUND OFF\nClick to toggle",1.35f,.63f);
        var pauseText=Button("PAUSE",new Vector3(1.5f,1.28f,-1.05f),"pause","START PAUSE\n48-second visual guide",1.35f,.63f);
        Text("01   LIGHT",new Vector3(-1.5f,1.8f,-1.15f),.07f,dark.color,controls);
        Text("02   SOUND",new Vector3(0,1.8f,-1.15f),.07f,dark.color,controls);
        Text("03   PAUSE",new Vector3(1.5f,1.8f,-1.15f),.07f,dark.color,controls);
        // World-space instruction boards; no screen-fixed menu.
        Board(new Vector3(-4.05f,2.05f,-.15f),"MAKE YOURSELF AT HOME",
            "Click a labelled control\n\n1   Warm / cool light\n2   Sound on / off\n3   Start / stop pause\n\nR   Reset the controls\nEsc   Stop sound + guide",2.3f,2.05f);
        Board(new Vector3(4.05f,2.05f,-.15f),"EXPLORE AT YOUR PACE",
            "W A S D   Move slowly\nHold right mouse   Look\nQ / E   Turn 30 degrees\n\nClick a floor pad to move\nHome   Return to entrance\n\nThe guide is optional.\nMove and look at any time.",2.3f,2.05f);
        Button("STOP",new Vector3(-4.05f,.68f,-.15f),"stop","STOP ALL\nSound + visual guide",1.7f,.42f);
        Button("RESET",new Vector3(4.05f,.68f,-.15f),"reset","RESET\nWarm / silent / stopped",1.7f,.42f);
        Pad(new Vector3(-3.6f,.04f,-3.8f),"MOVE HERE",new Vector3(-3.6f,.05f,-3.8f));
        Pad(new Vector3(3.6f,.04f,1.4f),"MOVE HERE",new Vector3(3.6f,.05f,1.4f));
        Text("DDES9902   /   WEEK 03   /   DESKTOP PROTOTYPE",new Vector3(0,.7f,-1.43f),.045f,cream.color,controls);
        var lampList=new List<Light>();var surfaceList=new List<Renderer>();
        foreach(int side in new[]{-1,1})
        {
            Primitive(PrimitiveType.Cylinder,"Lantern foot",new Vector3(side*3.4f,.075f,2.4f),new Vector3(.6f,.075f,.6f),wood,details);
            Primitive(PrimitiveType.Cylinder,"Lantern post",new Vector3(side*3.4f,1.15f,2.4f),new Vector3(.08f,1.1f,.08f),wood,details);
            var shade=Primitive(PrimitiveType.Cylinder,"Linen lantern",new Vector3(side*3.4f,2.3f,2.4f),new Vector3(.75f,.4f,.75f),glow,details,false);
            surfaceList.Add(shade.GetComponent<Renderer>());
            var lamp=new GameObject("Lantern light").AddComponent<Light>();lamp.transform.position=shade.transform.position;lamp.type=LightType.Point;lamp.range=6;lamp.transform.parent=details;lampList.Add(lamp);
        }
        var sunlight=new GameObject("Sunlight").AddComponent<Light>();sunlight.type=LightType.Directional;
        sunlight.transform.rotation=Quaternion.Euler(48,-28,0);sunlight.shadows=LightShadows.Soft;sunlight.intensity=1.15f;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.52f,.47f,.38f);
        var playerObject=new GameObject("04 Player - desktop preview");
        var character=playerObject.AddComponent<CharacterController>();character.height=1.7f;character.center=new Vector3(0,.85f,0);character.radius=.24f;character.stepOffset=.2f;
        playerObject.transform.position=new Vector3(0,.05f,-6.3f);
        var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";cameraObject.transform.parent=playerObject.transform;cameraObject.transform.localPosition=new Vector3(0,1.65f,0);
        var cam=cameraObject.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.84f,.75f,.61f);cam.fieldOfView=64;cam.nearClipPlane=.05f;cam.farClipPlane=100;
        cameraObject.AddComponent<AudioListener>();
        var experience=new GameObject("05 Experience logic").AddComponent<QuietCornerController>();
        experience.player=character;experience.viewCamera=cam;experience.orb=orb.transform;experience.sun=sunlight;experience.orbLight=orbLight;
        experience.lamps=lampList.ToArray();experience.lampSurfaces=surfaceList.ToArray();
        experience.lightStatus=lightText;experience.soundStatus=soundText;experience.pauseStatus=pauseText;experience.orbStatus=orbStatus;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=40;QualitySettings.antiAliasing=4;
        PlayerSettings.companyName="DDES9902 Prototype";PlayerSettings.productName="Quiet Corner";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/QuietCorner.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/QuietCorner.unity",true)};
        AssetDatabase.SaveAssets();
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0,1,0),Quaternion.Euler(15,0,0),9);
        Debug.Log("QUIET_CORNER_SCENE_READY");
    }

    public static void Build()
    {
        CreateScene();
        string path=Path.GetFullPath("../Week3_Submission/Playable/QuietCorner.exe");
        string[] args=System.Environment.GetCommandLineArgs();
        int outputIndex=System.Array.IndexOf(args,"-qc-build-output");
        if(outputIndex>=0 && outputIndex+1<args.Length) path=Path.GetFullPath(args[outputIndex+1]);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/QuietCorner.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("QUIET_CORNER_BUILD_READY "+path);
    }

    static Material Mat(string name,Color color,bool emission=false)
    {
        string path="Assets/Materials/"+name+".mat";
        Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.12f);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.6f);}
        return m;
    }
    static GameObject Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,Transform parent,bool collider=true)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.parent=parent;obj.transform.position=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject Box(string name,Vector3 p,Vector3 s,Material m,Transform parent,bool collider=true) { return Primitive(PrimitiveType.Cube,name,p,s,m,parent,collider); }
    static TextMesh Text(string text,Vector3 p,float size,Color color,Transform parent)
    {
        var obj=new GameObject(text.Split('\n')[0]);obj.transform.parent=parent;obj.transform.position=p;
        var tm=obj.AddComponent<TextMesh>();tm.text=text;tm.font=font;tm.fontSize=64;tm.characterSize=size*.3f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=color;tm.lineSpacing=1.15f;
        obj.GetComponent<MeshRenderer>().sharedMaterial=font.material;return tm;
    }
    static TextMesh Button(string name,Vector3 p,string action,string caption,float width,float height)
    {
        var button=Box(name+" - interactive",p,new Vector3(width,height,.15f),dark,controls);
        var target=button.AddComponent<QuietCornerTarget>();target.action=action;
        return Text(caption,p+new Vector3(0,0,-.082f),.062f,cream.color,button.transform);
    }
    static void Board(Vector3 p,string heading,string text,float width,float height)
    {
        Box("Instruction plaque",p,new Vector3(width,height,.12f),dark,controls);
        Text(heading,p+new Vector3(0,height*.38f,-.071f),.058f,new Color(.9f,.77f,.49f),controls);
        Text(text,p+new Vector3(0,-.1f,-.071f),.06f,cream.color,controls);
    }
    static void Pad(Vector3 p,string label,Vector3 destination)
    {
        var pad=Primitive(PrimitiveType.Cylinder,"Floor move target",p,new Vector3(1.1f,.022f,1.1f),teal,controls);
        var target=pad.AddComponent<QuietCornerTarget>();target.action="teleport";target.destination=destination;
        var labelText=Text(label,p+new Vector3(0,.027f,0),.052f,cream.color,controls);labelText.transform.rotation=Quaternion.Euler(90,0,0);
    }
    static void Plant(Vector3 p)
    {
        Primitive(PrimitiveType.Cylinder,"Terracotta planter",p+Vector3.up*.25f,new Vector3(.6f,.25f,.6f),wood,details);
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI/3;
            var frond=Primitive(PrimitiveType.Sphere,"Plant leaf",p+new Vector3(Mathf.Cos(a)*.25f,.85f+i%2*.25f,Mathf.Sin(a)*.25f),new Vector3(.28f,1f,.14f),leaf,details,false);
            frond.transform.rotation=Quaternion.Euler(Mathf.Sin(a)*30,0,Mathf.Cos(a)*30);
        }
    }
    static void Tree(Vector3 p)
    {
        Primitive(PrimitiveType.Cylinder,"Garden trunk",p+Vector3.up*1.6f,new Vector3(.22f,1.6f,.22f),wood,details,false);
        Primitive(PrimitiveType.Sphere,"Garden crown",p+Vector3.up*3.2f,new Vector3(2.4f,3,2.4f),leaf,details,false);
    }
}
