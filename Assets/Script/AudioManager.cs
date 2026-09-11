using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public AudioClip Main;
    public AudioClip WestHill;
    public AudioClip YourBed;
    public AudioClip Bar;
    public AudioClip Park;
    public AudioClip Oak;
    public AudioClip Home;
    public AudioClip Downtown;
    public AudioClip Sea;
    public AudioClip factory;
    public AudioClip Mc;
    public AudioClip Abyssclub;
    public AudioClip Underground;
    public AudioClip UndergroundHome;
    public AudioClip UndergroundDowntown;
    public AudioClip UndergroundBook;
    public AudioClip Supermarket;
    public AudioClip Cinema;
    public AudioClip Museum;
    public AudioClip EditorOffice;
    public AudioClip BookStore;
    public AudioClip NewDistrict;
    public AudioClip Shipyard;
    public AudioClip OverGround;

    public AudioSource audiosource;
    private string currentNodeId;
    private bool mainMusicActive;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (audiosource == null)
            audiosource = GetComponent<AudioSource>();
        if (audiosource == null)
            audiosource = gameObject.AddComponent<AudioSource>();
    }

    public void SetMainMusicActive(bool active)
    {
        mainMusicActive = active;
        if (mainMusicActive)
            PlayClip(Main);
        else
            PlayNode(currentNodeId);
    }

    public void PlayNode(string nodeId)
    {
        currentNodeId = nodeId;
        if (mainMusicActive)
            return;

        PlayClip(GetNodeClip(nodeId));
    }

    private void PlayClip(AudioClip clip)
    {
        if (audiosource == null)
            return;

        if (clip == null)
        {
            audiosource.Stop();
            audiosource.clip = null;
            return;
        }

        if (audiosource.clip == clip && audiosource.isPlaying)
            return;

        audiosource.Stop();
        audiosource.clip = clip;
        audiosource.loop = true;
        audiosource.Play();
    }

    private AudioClip GetNodeClip(string nodeId)
    {
        switch (nodeId)
        {
            case "m1": return WestHill;
            case "m2": return Home;
            case "m3": return YourBed;
            case "m4": return Bar;
            case "m5": return Park;
            case "m6": return Supermarket;
            case "m7": return UndergroundHome != null ? UndergroundHome : Underground;
            case "m8": return Downtown;
            case "m9": return Abyssclub;
            case "m10": return Cinema;
            case "m11": return Mc;
            case "m12": return UndergroundDowntown != null ? UndergroundDowntown : Underground;
            case "m13": return Oak;
            case "m14": return Museum;
            case "m15": return EditorOffice;
            case "m16": return BookStore;
            case "m17": return UndergroundBook != null ? UndergroundBook : Underground;
            case "m18": return NewDistrict;
            case "m19": return Shipyard != null ? Shipyard : factory;
            case "m20": return Sea;
            case "m21": return OverGround;
            default: return null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


}
