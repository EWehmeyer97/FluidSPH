static const float PI = 3.1415926;


// 3d conversion: done
float SmoothingKernelPoly6(float dst, float radius, float radius9)
{
	if (dst < radius)
	{
		float scale = 315 / (64 * PI * radius9);
		float v = radius * radius - dst * dst;
		return v * v * v * scale;
	}
	return 0;
}

// 3d conversion: done
float SpikyKernelPow3(float dst, float radius, float radius6)
{
	if (dst < radius)
	{
		float scale = 15 / (PI * radius6);
		float v = radius - dst;
		return v * v * v * scale;
	}
	return 0;
}

// 3d conversion: done
//Integrate[(h-r)^2 r^2 Sin[θ], {r, 0, h}, {θ, 0, π}, {φ, 0, 2*π}]
float SpikyKernelPow2(float dst, float radius, float radius5)
{
	if (dst < radius)
	{
		float scale = 15 / (2 * PI * radius5);
		float v = radius - dst;
		return v * v * scale;
	}
	return 0;
}

// 3d conversion: done
float DerivativeSpikyPow3(float dst, float radius, float radius6)
{
	if (dst <= radius)
	{
		float scale = 45 / (radius6 * PI);
		float v = radius - dst;
		return -v * v * scale;
	}
	return 0;
}

// 3d conversion: done
float DerivativeSpikyPow2(float dst, float radius, float radius5)
{
	if (dst <= radius)
	{
		float scale = 15 / (radius5 * PI);
		float v = radius - dst;
		return -v * scale;
	}
	return 0;
}

float DensityKernel(float dst, float radius, float radius5)
{
	//return SmoothingKernelPoly6(dst, radius);
	return SpikyKernelPow2(dst, radius, radius5);
}

float NearDensityKernel(float dst, float radius, float radius6)
{
	return SpikyKernelPow3(dst, radius, radius6);
}

float DensityDerivative(float dst, float radius, float radius5)
{
	return DerivativeSpikyPow2(dst, radius, radius5);
}

float NearDensityDerivative(float dst, float radius, float radius6)
{
	return DerivativeSpikyPow3(dst, radius, radius6);
}

