shader_type canvas_item;

uniform bool UseColor;
uniform sampler2D PreviousTexture : hint_default_transparent;
uniform sampler2D DissolveTexture;
uniform float TransitionValue;

void fragment() {
	vec4 mainTex;
	if (UseColor)
	{
		mainTex = COLOR;
		mainTex.a = min(TransitionValue, mainTex.a);
	}
	else
	{
	   	mainTex = texture(TEXTURE, UV);
		mainTex.a = min(TransitionValue, texture(TEXTURE, UV).a);
	}
	COLOR = mainTex;
}