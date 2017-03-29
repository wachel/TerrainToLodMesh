using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestScritp : MonoBehaviour
{
    public Terrain terrain;
    public Transform mesh;
    Button btnSwitch;
    Text txtFPS;
    bool currentIsTerrain = false;

    public void Awake()
    {
        btnSwitch = transform.Find("Canvas/BtnSwitch").GetComponent<Button>();
        txtFPS = transform.Find("Canvas/TxtFPS").GetComponent<Text>();
        btnSwitch.onClick.AddListener(() => {
            currentIsTerrain = !currentIsTerrain;
            UpdateSwitch();
        });
        UpdateSwitch();
    }

    public void UpdateSwitch()
    {
        terrain.gameObject.SetActive(currentIsTerrain);
        mesh.gameObject.SetActive(!currentIsTerrain);

        btnSwitch.GetComponentInChildren<Text>().text = currentIsTerrain ? "Current Is Terrain" : "Current Is Mesh";
    }

    float nextFpsTime;
    int frame;
    public void Update()
    {
        frame++;
        if(Time.time > nextFpsTime) {
            txtFPS.text = ((int)(frame / 0.5f)).ToString();
            nextFpsTime = Time.time + 0.5f;
            frame = 0;
        }

    }
}
