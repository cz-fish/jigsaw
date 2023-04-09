using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JigsawPiece : MonoBehaviour
{
    private bool m_dragging = false;
    private bool m_placed = false;
    private Vector3 m_pointerOffset;
    private Camera m_camera;

    private const float c_zPlaced = 0.125f;
    private const float c_zScattered = 0f;
    private const float c_zDragging = -0.125f;

    [SerializeField] public int row;
    [SerializeField] public int column;
    [SerializeField] public Vector3 targetPosition;
    [SerializeField] public GameObject dropSlot;

    public JigsawGame m_game;

    public void Awake()
    {
        m_camera = Camera.main;
    }

    public void OnMouseDown()
    {
        if (m_placed) {
            return;
        }
        m_dragging = true;
        m_pointerOffset = transform.position - m_camera.ScreenToWorldPoint(Input.mousePosition);
    }

    public void OnMouseUp()
    {
        if (!m_dragging) {
            return;
        }
        m_dragging = false;

        var (position, isPlaced) = m_game.DropPiece(this);
        if (isPlaced) {
            // Piece is snapped in its position
            position.z = c_zPlaced;
            m_placed = true;
            // Disable the dropSlot
            dropSlot.SetActive(false);
        } else {
            // Not in correct position, back to scattered state
            position.z = c_zScattered;
        }
        transform.position = position;
        //transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    public void Update()
    {
        if (!m_dragging) {
            return;
        }

        // TODO: let m_game know about this drag in case multiple pieces are being moved together

        var position = m_camera.ScreenToWorldPoint(Input.mousePosition) + m_pointerOffset;
        // Lift draggable piece above others
        position.z = c_zDragging;
        transform.position = position;
    }
}
