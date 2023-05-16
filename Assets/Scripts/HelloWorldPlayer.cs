using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {

        public NetworkVariable<int> choosedColor = new NetworkVariable<int>();
        public NetworkList<int> usedColors;

        public List<Material> materials = new List<Material>();

        private HelloWorldManager helloWorldManager;

        private float movingDistance = 0.1f;
        private Rigidbody rb;

        // Evitar que elimine a cor 0 (cor anterior) se, por casualidade,
        // fora a elixida aleatoriamente ao spanearse
        private bool firstColorChange = true;

        void Awake() {
            helloWorldManager = GameObject.Find("HelloWorldManager").GetComponent<HelloWorldManager>();
            usedColors = new NetworkList<int>();
            rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn() 
        {
            if (IsOwner)             {
                SubmitInitialPositionRequestServerRpc();
                ChangeColor();
            }
        }

        public void ChangeColor()
        {
            SubmitChangeColorServerRpc(); 
        }

        [ServerRpc]
        void SubmitChangeColorServerRpc(){
            
            int newColor = -1;  //obrigar a entrar no bucle while
            int oldColor = choosedColor.Value;

            // Escollemos unha cor libre aleatoriamente
            while (newColor < 0)  {
                newColor = Random.Range(0, materials.Count);
                if (helloWorldManager.usedColors.Contains(newColor)) {
                    newColor = -1;
                }
            }

            helloWorldManager.AddColor(newColor);
            if (! firstColorChange) {
                helloWorldManager.RemoveColor(oldColor);
            } else {                
                firstColorChange = false;
            }
            choosedColor.Value = newColor;

        }

        [ServerRpc]
        void SubmitInitialPositionRequestServerRpc()
        {
            transform.position = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        }

        [ServerRpc]
        void SubmitPositionServerRpc(float moveLeftRight, float moveBackForward){
            transform.position = new Vector3(transform.position.x + moveLeftRight, transform.position.y, transform.position.z + moveBackForward);
        }

        [ServerRpc]
        void SubmitPositionJumpingServerRpc() {
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
        }
        
        
        void Update()
        {
            if (IsOwner) {
                if (Input.GetKeyDown(KeyCode.LeftArrow))   SubmitPositionServerRpc(- movingDistance, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow))  SubmitPositionServerRpc(movingDistance, 0);
                if (Input.GetKeyDown(KeyCode.UpArrow))     SubmitPositionServerRpc(0, movingDistance);
                if (Input.GetKeyDown(KeyCode.DownArrow))   SubmitPositionServerRpc(0, -movingDistance);

                if (Input.GetKeyDown(KeyCode.Space)) SubmitPositionJumpingServerRpc();
            }

            GetComponent<MeshRenderer>().material = materials[choosedColor.Value];
        }

        void Start() {
            if (IsOwner) {
                ChangeColor();
            }
        }
    }
}