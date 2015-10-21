﻿using System;

namespace UnityServer
{
	public struct Quaternion
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public Quaternion(float x, float y, float z, float w){
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}
}

