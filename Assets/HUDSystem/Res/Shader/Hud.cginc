// 如果两个数字相等为1 不相等为0
float compareEquals(float a, float b)
{
    float threshold = 0.01;
    float diff = abs(a - b);
    float result = step(threshold, 1.0 - diff);
    return result;
}

float f16tof32(uint x)
{
    const uint shifted_exp = (0x7c00 << 13);
    uint uf = (x & 0x7fff) << 13;
    uint e = uf & shifted_exp;
    uf += (127 - 15) << 23;
    uf += lerp(0, (128u - 16u) << 23, compareEquals(e, shifted_exp));
    uf = lerp(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), compareEquals(e, 0));
    uf |= (x & 0x8000) << 16;
    return asfloat(uf);
}

//uint select(uint a, uint b, bool c) { return c ? b : a; }
//
//float f16tof32(uint x)
//{
//    const uint shifted_exp = (0x7c00 << 13);
//    uint uf = (x & 0x7fff) << 13;
//    uint e = uf & shifted_exp;
//    uf += (127 - 15) << 23;
//    uf += select(0, (128u - 16u) << 23, e == shifted_exp);
//    uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0);
//    uf |= (x & 0x8000) << 16;
//    return asfloat(uf);
//}

float2 toFloat2(float fv)
{
    uint uv = asuint(fv);
    uint uv2 = uv & 0x0000ffff;
    uint uv1 = uv >> 16;
    return float2(f16tof32(uv1), f16tof32(uv2));
}

float float4x4Value(int paramIndex, float4x4 param)
{
    int h = fmod(paramIndex, 4);
    int w = floor(paramIndex / 4.0);
    return param[h][w];
}

int getSpriteId(int index, float4x4 param)
{
    int valueindex = floor(index / 2.0);
    int parity = fmod(index, 2);
    float value = float4x4Value(9 + valueindex, param);
    float2 curfv = toFloat2(value);
    return curfv[parity] - 1;
}

float2 getSpritePosition(int index, float4x4 param)
{
    float v = float4x4Value(index, param);
    return toFloat2(v);
}

float2 getSpriteSize(int index, float4x4 param)
{
    float v = float4x4Value(index, param);
    return toFloat2(v);
}

fixed4 float2ToColor(float rg, float ba)
{
    float2 rgvalue = toFloat2(rg);
    float2 bavalue = toFloat2(ba);
    return fixed4(rgvalue[0], rgvalue[1], bavalue[0], bavalue[1]);
}

void rotate2D(inout float2 v, float r)
{
    float s, c;
    sincos(r, s, c);
    v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float2 getAtlasMapping(int index, float width, float height, sampler2D atlasMappingTex)
{
    int w = fmod(index, width);
    int h = int(index / width);
    float2 uv = float2(w / width, h / height);
    float4 col = tex2Dlod(atlasMappingTex, float4(uv, 0, 0));
    col = col * 255;
    float v1 = (col.r * 256 + col.g);
    float v2 = (col.b * 256 + col.a);
    return float2(v1, v2);
}