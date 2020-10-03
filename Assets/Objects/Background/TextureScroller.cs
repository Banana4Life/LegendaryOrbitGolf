using UnityEngine;

namespace Objects.Background
{
	[RequireComponent(typeof(Renderer))]
	public class TextureScroller : MonoBehaviour
	{
		public float scrollSpeed = 1;
		public Camera playerCamera;
		private Renderer _renderer;
		private static readonly int MainTex = Shader.PropertyToID("_MainTex");
		private Vector2 _lastCameraPos;

		private void Start()
		{
			_renderer = GetComponent<Renderer>();
			_lastCameraPos = CameraPosition();
		}

		private Vector2 CameraPosition()
		{
			var pos = playerCamera.transform.position;
			return new Vector2(pos.x, pos.z);
		}

		private Vector2 CameraDelta()
		{
			var newPos = CameraPosition();
			var delta = newPos - _lastCameraPos;
			_lastCameraPos = newPos;
			return delta;
		}

		void Update ()
		{
			var mat = _renderer.material;
			var currentOffset = mat.GetTextureOffset(MainTex);
			var newOffset = CameraDelta() * (Time.deltaTime * scrollSpeed);
			mat.SetTextureOffset(MainTex, currentOffset - newOffset);
			var localScale = transform.localScale;
			
			mat.SetTextureScale(MainTex, new Vector2(localScale.x, localScale.z));
		}
	}
}
