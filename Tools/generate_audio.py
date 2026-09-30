"""Original deterministic synthesis for Abyss. Python standard library only."""
from pathlib import Path
import math, random, wave, array, json
ROOT=Path(__file__).resolve().parent.parent/'Audio'
ROOT.mkdir(exist_ok=True)
RATE=22050
TAU=math.tau

def save(name, samples):
    peak=max(abs(x) for x in samples) or 1
    gain=min(1,.78/peak)
    pcm=array.array('h',(int(max(-1,min(1,x*gain))*32767) for x in samples))
    import sys
    if sys.byteorder!='little': pcm.byteswap()
    with wave.open(str(ROOT/(name+'.wav')),'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(RATE); f.writeframes(pcm.tobytes())
    return {'seconds':round(len(samples)/RATE,3),'peak':round(peak*gain,4)}

# Short pulse/triangle/noise gestures, softly enveloped to prevent clicks.
SOUNDS={
 'step':(.065,95,45,.65), 'swing':(.13,330,75,.55), 'miss':(.10,180,80,.45),
 'herohurt':(.18,170,48,.4), 'enemyhurt':(.10,240,65,.5), 'arrow':(.14,900,180,.65),
 'arcanebolt':(.24,240,950,.05), 'whirlwind':(.42,160,520,.35), 'arcanenova':(.55,130,1000,.10),
 'piercingarrow':(.32,1400,130,.4), 'shadowstep':(.25,420,60,.2), 'torch':(.2,200,65,.7),
 'seismicimpact':(.5,110,30,.45), 'floodwave':(.48,360,80,.7), 'sporeburst':(.42,220,660,.2),
 'furnacecross':(.5,150,440,.65), 'pickup':(.16,700,1200,0), 'chest':(.32,180,580,.15),
 'potion':(.3,280,850,.04), 'equip':(.14,350,220,.1), 'levelup':(.65,330,990,0),
 'stairs':(.38,520,180,.05), 'death':(.85,260,38,.08), 'menu':(.045,520,600,0),
 'confirm':(.12,440,880,0), 'trap':(.24,1100,90,.3), 'break':(.16,140,40,.8),
 'wardencharge':(.55,65,240,.1)
}
manifest={}
for index,(name,(duration,start,end,noise)) in enumerate(SOUNDS.items()):
    rng=random.Random(500+index); samples=[]; phase=0; held=0
    for i in range(int(RATE*duration)):
        t=i/RATE; p=t/duration
        # Stair-stepped pitch and sample-and-hold noise evoke early sound chips.
        frequency=start*((end/start)**(int(p*16)/16))
        phase+=frequency/RATE
        tone=.65*(1 if phase%1<.25 else -1)+.35*(1-4*abs(phase%1-.5))
        if i%5==0: held=rng.uniform(-1,1)
        envelope=min(1,t/.004)*min(1,(duration-t)/.012)*(1-p)**.7
        if name in ('whirlwind','sporeburst','levelup'): envelope*=.55+.45*math.sin(TAU*t*12)**2
        samples.append(round((tone*(1-noise)+held*noise)*envelope*36)/64)
    manifest[name]=save(name,samples)

# Forty-second minor/modal loops. Slow drones, isolated bell tones and diffuse
# echoes leave generous silence around the melody; there is no busy beat.
TRACKS={
 'menu':(38,[0,7,10,3,2,7,0,5],.7),
 'ruins':(38,[0,7,3,2,0,10,7,2],.55),
 'cistern':(36,[0,7,2,10,5,2,7,0],.85),
 'fungal':(41,[0,1,7,3,8,7,1,0],.5),
 'forge':(33,[0,7,1,0,6,7,3,1],.35),
 'refuge':(43,[0,7,3,10,7,5,3,0],.9),
 'warden':(33,[0,1,7,6,0,3,1,0],.25)
}
for name,(root,notes,brightness) in TRACKS.items():
    length=40; n=RATE*length; samples=[0.0]*n
    base=440*2**((root-69)/12)
    # Integer-cycle drone frequencies make the wrap continuous.
    low=round(base*length)/length; fifth=round(base*1.5*length)/length
    for i in range(n):
        t=i/RATE
        breath=.75+.25*math.cos(TAU*t/length)
        pulse=(.82+.18*math.cos(TAU*t*1.2)) if name=='warden' else 1
        samples[i]=(.085*math.sin(TAU*low*t)+.035*math.sin(TAU*fifth*t)+.018*math.sin(TAU*(low+.1)*t))*breath*pulse
    for j,note in enumerate(notes):
        onset=j*5+1.2; freq=base*2*2**(note/12)
        for i in range(int(RATE*7)):
            t=i/RATE; env=(1-math.exp(-t*9))*math.exp(-t*.85)
            bell=(math.sin(TAU*freq*t)+brightness*.3*math.sin(TAU*freq*2*t)*math.exp(-t*1.8))*.12*env
            for delay,gain in ((0,1),(.43,.22),(1.07,.12),(1.83,.06)):
                samples[(int((onset+delay)*RATE)+i)%n]+=bell*gain
    manifest[name]=save('music_'+name,samples)
ROOT.mkdir(exist_ok=True)
(ROOT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Generated',len(SOUNDS),'effects and',len(TRACKS),'music loops.')
