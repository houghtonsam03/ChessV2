using UnityEngine;

public class ChessSound : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip gameStart;
    public AudioClip moveWhite;
    public AudioClip moveBlack;
    public AudioClip capture;
    public AudioClip promotion;
     public AudioClip castle;
    public AudioClip check;
    public AudioClip gameOver;
    public AudioClip timeout;
    public AudioClip illegal;
    public AudioClip premove;

    public struct Sound
    {
        public const int Start = 0;
        public const int MoveWhite = 1;
        public const int MoveBlack = 2;
        public const int Capture = 3;
        public const int Promotion = 4;
        public const int Castle = 5;
        public const int Check = 6;
        public const int End = 7;
        public const int TimeOut = 8;
        public const int Premove = 9;
        public const int Illegal = 10;
    }
    public void SetSpeaker(AudioSource audioS)
    {
        audioSource = audioS;
    }
    public void PlayMoveSound(int s)
    {
        if (s == Sound.Check) PlayClip(check);
        else if (s == Sound.Castle) PlayClip(castle);
        else if (s == Sound.Promotion) PlayClip(promotion);
         else if (s == Sound.Capture) PlayClip(capture);
        else if (s == Sound.MoveWhite) PlayClip(moveWhite);
        else PlayClip(moveBlack);
    }
    public void PlayStartSound()
    {
        PlayClip(gameStart);
    }
    public void PlayGameoverSound()
    {
        PlayClip(gameOver);
    }
    public void PlayTimeoutSound()
    {
        PlayClip(timeout);
    }
    public void PlayIllegalSound()
    {
        PlayClip(illegal);
    }
    public void PlayPremoveSound()
    {
        PlayClip(premove);
    }
    public void PlayClip(AudioClip clip)
    {
        if (!audioSource.isPlaying)
        {
            audioSource.clip = clip;
            audioSource.Play();
            return;
        }
        float progress = audioSource.time / audioSource.clip.length;
        if (progress > 0.3f)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}