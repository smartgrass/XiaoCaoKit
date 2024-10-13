using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Test_UIColor : MonoBehaviour
{
    public Color bgColor = Color.white;
    public Color BtnbgColor = Color.white;
    public Color textColor = Color.black;
    public Color btnColor = Color.black;




    [Button]
    void SetColor()
    {
        foreach (Transform tf in transform.GetComponentsInChildren<Transform>())
        {
            if (tf.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI tmp))
            {
                tmp.color = textColor;
            }

            else if (tf.TryGetComponent(out Button btn) && tf.TryGetComponent(out Image img))
            {
                img.color = btnColor;
            }
            else if (tf.gameObject.name == ("BtnBg") && tf.TryGetComponent(out Image img2))
            {
                img2.color = BtnbgColor;
            }
            else if (tf.gameObject.name.Contains("Bg") && tf.TryGetComponent(out Image img3))
            {
                img3.color = bgColor;
            }
        }
    }

    [Button]
    void LoadColor()
    {
        var tfs = transform.GetComponentsInChildren<Transform>();

        foreach (Transform tf in tfs)
        {
            if (tf.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI tmp))
            {
                textColor = tmp.color;
                break;
            }
        }
        foreach (Transform tf in tfs)
        {
            if (tf.TryGetComponent(out Button btn) && tf.TryGetComponent(out Image img))
            {
                btnColor = img.color;
                break;
            }
        }

        foreach (Transform tf in tfs)
        {
            if (tf.gameObject.name == ("BtnBg") && tf.TryGetComponent(out Image img2))
            {
                BtnbgColor = img2.color;
                break;
            }
        }

        foreach (Transform tf in tfs)
        {
            if (tf.gameObject.name.Contains("Bg") && tf.TryGetComponent(out Image img3))
            {
                bgColor = img3.color;
                break;
            }
        }
    }


}
