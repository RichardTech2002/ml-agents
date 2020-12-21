using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushBlockEnvController : MonoBehaviour
{
    [System.Serializable]
    public class AgentInfo
    {
        public PushAgentCollab Agent;
        public Vector3 StartingPos;
        public Quaternion StartingRot;
        public Rigidbody Rb;

    }

    [System.Serializable]
    public class BlockInfo
    {
        public Transform T;
        public Vector3 StartingPos;
        public Quaternion StartingRot;
        public Rigidbody Rb;
    }

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    public GameObject area;

    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    public List<AgentInfo> AgentsList = new List<AgentInfo>();
    public List<BlockInfo> BlocksList = new List<BlockInfo>();

    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    public bool UseRandomBlockRotation = true;
    public bool UseRandomBlockPosition = true;
    PushBlockSettings m_PushBlockSettings;

    private int m_NumberOfRemainingBlocks;

    void Start()
    {
        m_NumberOfRemainingBlocks = BlocksList.Count;

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        foreach (var item in BlocksList)
        {
            item.StartingPos = item.T.transform.position;
            item.StartingRot = item.T.transform.rotation;
            item.Rb = item.T.GetComponent<Rigidbody>();
        }
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
        }

        ResetScene();

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock(BlockInfo block)
    {
        // Get a random position for the block.
        block.T.position = GetRandomSpawnPos();

        // Reset block velocity back to zero.
        block.Rb.velocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        block.Rb.angularVelocity = Vector3.zero;
    }


    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Called when the agent moves the block into the goal.
    /// </summary>
    public void ScoredAGoal(Collider col)
    {
        m_NumberOfRemainingBlocks--;
        bool done = m_NumberOfRemainingBlocks == 0;

        //Give Agent Rewards
        foreach (var item in AgentsList)
        {
            item.Agent.AddReward(5f);
        }

        // Swap ground material for a bit to indicate we scored.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));

        if (done)
        {
            //Reset assets
            ResetScene();
        }
    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    void ResetScene()
    {
        //Random platform rot
        area.transform.rotation = GetRandomRot();

        //End Episode
        foreach (var item in AgentsList)
        {
            if (!item.Agent)
            {
                return;
            }
            item.Agent.EndEpisode();
        }
        //Reset Agents
        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Blocks
        foreach (var item in BlocksList)
        {
            var pos = UseRandomBlockPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomBlockRotation ? GetRandomRot() : item.StartingRot;

            item.T.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }
    }
}
