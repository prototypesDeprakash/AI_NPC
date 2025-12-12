using UnityEngine;

public class DiscoFogRGB : MonoBehaviour
{
    [SerializeField] float speed = 10f; // how fast we switch colors
    public bool policsChasingLightEffect = false;



    void Update()
    {
        if(policsChasingLightEffect)
        {
            PolicChaseEffect();
        }
        else
        {
            DiscoEffect();
        }

    }

    private void DiscoEffect()
    {
        float t = Time.time * speed;

        // harsh RGB cycles, no smoothing
        float r = Mathf.Abs(Mathf.Sin(t));
        float g = Mathf.Abs(Mathf.Sin(t * 1.3f));
        float b = Mathf.Abs(Mathf.Sin(t * 1.7f));

        RenderSettings.fogColor = new Color(r, g, b, 1f);
    }

    private void PolicChaseEffect()
    {
        

        float t = Mathf.PingPong(Time.time * speed, 1f);

        // Red & Dark Blue
        Color red = new Color(1f, 0f, 0f);
        Color blue = new Color(0f, 0f, 1f); // dark blue

        // Lerping between them
        RenderSettings.fogColor = Color.Lerp(red, blue, t);

    }

}
