shader_type canvas_item;

uniform bool UseColor;
uniform sampler2D DissolveTexture;
uniform float TransitionValue : hint_range(0,1);

void fragment() {
	 vec4 mainTex;
	if (UseColor)
		mainTex = COLOR;
	else
    	mainTex = texture(TEXTURE, UV);
    vec4 noiseTex = texture(DissolveTexture, UV);
    mainTex.a *= floor(TransitionValue + min(1, noiseTex.x));
    COLOR = mainTex;
}