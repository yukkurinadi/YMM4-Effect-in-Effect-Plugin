// エフェクト内エフェクト用：差分マスク合成シェーダー
// input0: 元画像(I)
// input1: かけられるエフェクト適用後(T)
// input2: かけるエフェクト適用後(J)
// TとIの差分があるピクセル（＝かけられるエフェクトが変化させた部分）だけJを採用し、
// それ以外はIをそのまま返す。premultiplied alpha前提。

Texture2D InputTexture0 : register(t0);
SamplerState InputSampler0 : register(s0);
Texture2D InputTexture1 : register(t1);
SamplerState InputSampler1 : register(s1);
Texture2D InputTexture2 : register(t2);
SamplerState InputSampler2 : register(s2);

float4 main(
	float4 pos : SV_POSITION,
	float4 posScene : SCENE_POSITION,
	float4 uv0 : TEXCOORD0,
	float4 uv1 : TEXCOORD1,
	float4 uv2 : TEXCOORD2
) : SV_Target
{
	float4 src = InputTexture0.Sample(InputSampler0, uv0.xy);
	float4 tgt = InputTexture1.Sample(InputSampler1, uv1.xy);
	float4 inner = InputTexture2.Sample(InputSampler2, uv2.xy);

	float4 diff = abs(tgt - src);
	float lum = 0.299 * diff.r + 0.587 * diff.g + 0.114 * diff.b;
	float mask = saturate((lum + diff.a) * 4.0);

	return lerp(src, inner, mask);
}
