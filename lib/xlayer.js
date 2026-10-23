/* ============ X: Szenen-, Geometrie- und Materialschicht auf der PlayCanvas-Engine ============ */
const X=(()=>{
const DEG=180/Math.PI;
let app=null, device=null;
/* ---------- Mathe ---------- */
class V2{ constructor(x=0,y=0){ this.x=x; this.y=y; } set(x,y){ this.x=x; this.y=y; return this; } }
class V3{
  constructor(x=0,y=0,z=0){ this.x=x; this.y=y; this.z=z; }
  set(x,y,z){ this.x=x; this.y=y; this.z=z; return this; }
  setScalar(s){ this.x=this.y=this.z=s; return this; }
  copy(v){ this.x=v.x; this.y=v.y; this.z=v.z; return this; }
  clone(){ return new V3(this.x,this.y,this.z); }
  add(v){ this.x+=v.x; this.y+=v.y; this.z+=v.z; return this; }
  sub(v){ this.x-=v.x; this.y-=v.y; this.z-=v.z; return this; }
  subVectors(a,b){ this.x=a.x-b.x; this.y=a.y-b.y; this.z=a.z-b.z; return this; }
  addScaledVector(v,s){ this.x+=v.x*s; this.y+=v.y*s; this.z+=v.z*s; return this; }
  multiplyScalar(s){ this.x*=s; this.y*=s; this.z*=s; return this; }
  length(){ return Math.sqrt(this.x*this.x+this.y*this.y+this.z*this.z); }
  lengthSq(){ return this.x*this.x+this.y*this.y+this.z*this.z; }
  normalize(){ const l=this.length()||1; return this.multiplyScalar(1/l); }
  dot(v){ return this.x*v.x+this.y*v.y+this.z*v.z; }
  crossVectors(a,b){ const x=a.y*b.z-a.z*b.y, y=a.z*b.x-a.x*b.z, z=a.x*b.y-a.y*b.x; this.x=x; this.y=y; this.z=z; return this; }
  cross(v){ return this.crossVectors(this,v); }
  lerp(v,t){ this.x+=(v.x-this.x)*t; this.y+=(v.y-this.y)*t; this.z+=(v.z-this.z)*t; return this; }
  distanceTo(v){ return Math.hypot(this.x-v.x,this.y-v.y,this.z-v.z); }
  applyQuaternion(q){ const vx=this.x,vy=this.y,vz=this.z, qx=q.x,qy=q.y,qz=q.z,qw=q.w; const tx=2*(qy*vz-qz*vy), ty=2*(qz*vx-qx*vz), tz=2*(qx*vy-qy*vx); this.x=vx+qw*tx+qy*tz-qz*ty; this.y=vy+qw*ty+qz*tx-qx*tz; this.z=vz+qw*tz+qx*ty-qy*tx; return this; }
  applyMatrix4(m){ const e=m.elements, x=this.x,y=this.y,z=this.z; const w=1/((e[3]*x+e[7]*y+e[11]*z+e[15])||1); this.x=(e[0]*x+e[4]*y+e[8]*z+e[12])*w; this.y=(e[1]*x+e[5]*y+e[9]*z+e[13])*w; this.z=(e[2]*x+e[6]*y+e[10]*z+e[14])*w; return this; }
  project(cam){ // -> NDC wie gewohnt (z>1 = hinter der Kamera)
    cam.updateWorld(); const e=cam.matrixWorld.elements; const dx=this.x-e[12], dy=this.y-e[13], dz=this.z-e[14];
    const vx=dx*e[0]+dy*e[1]+dz*e[2], vy=dx*e[4]+dy*e[5]+dz*e[6], vz=dx*e[8]+dy*e[9]+dz*e[10];
    const f=1/Math.tan(cam.fov*Math.PI/360), d=-vz; if(d<=cam.near){ this.x=0; this.y=0; this.z=2; return this; }
    this.x=f/cam.aspect*vx/d; this.y=f*vy/d; this.z=.5; return this; }
}
class Eu{ constructor(x=0,y=0,z=0,o='XYZ'){ this.x=x; this.y=y; this.z=z; this.order=o; } set(x,y,z,o){ this.x=x; this.y=y; this.z=z; if(o) this.order=o; return this; } copy(e){ this.x=e.x; this.y=e.y; this.z=e.z; this.order=e.order; return this; } }
class Q{
  constructor(x=0,y=0,z=0,w=1){ this.x=x; this.y=y; this.z=z; this.w=w; this.v=0; }
  _b(){ this.v++; return this; }
  set(x,y,z,w){ this.x=x; this.y=y; this.z=z; this.w=w; return this._b(); }
  identity(){ return this.set(0,0,0,1); }
  copy(q){ this.x=q.x; this.y=q.y; this.z=q.z; this.w=q.w; return this._b(); }
  clone(){ return new Q(this.x,this.y,this.z,this.w); }
  _euler(e){ const c1=Math.cos(e.x/2),c2=Math.cos(e.y/2),c3=Math.cos(e.z/2),s1=Math.sin(e.x/2),s2=Math.sin(e.y/2),s3=Math.sin(e.z/2);
    if(e.order==='YXZ'){ this.x=s1*c2*c3+c1*s2*s3; this.y=c1*s2*c3-s1*c2*s3; this.z=c1*c2*s3-s1*s2*c3; this.w=c1*c2*c3+s1*s2*s3; }
    else { this.x=s1*c2*c3+c1*s2*s3; this.y=c1*s2*c3-s1*c2*s3; this.z=c1*c2*s3+s1*s2*c3; this.w=c1*c2*c3-s1*s2*s3; } return this; }
  setFromEuler(e){ this._euler(e); return this._b(); }
  setFromAxisAngle(a,ang){ const s=Math.sin(ang/2); this.x=a.x*s; this.y=a.y*s; this.z=a.z*s; this.w=Math.cos(ang/2); return this._b(); }
  setFromUnitVectors(a,b){ let r=a.dot(b)+1;
    if(r<1e-6){ r=0; if(Math.abs(a.x)>Math.abs(a.z)){ this.x=-a.y; this.y=a.x; this.z=0; this.w=r; } else { this.x=0; this.y=-a.z; this.z=a.y; this.w=r; } }
    else { this.x=a.y*b.z-a.z*b.y; this.y=a.z*b.x-a.x*b.z; this.z=a.x*b.y-a.y*b.x; this.w=r; }
    return this.normalize(); }
  normalize(){ let l=Math.hypot(this.x,this.y,this.z,this.w); if(!l){ this.x=this.y=this.z=0; this.w=1; } else { l=1/l; this.x*=l; this.y*=l; this.z*=l; this.w*=l; } return this._b(); }
  invert(){ this.x=-this.x; this.y=-this.y; this.z=-this.z; return this._b(); }
  multiplyQuaternions(a,b){ const ax=a.x,ay=a.y,az=a.z,aw=a.w,bx=b.x,by=b.y,bz=b.z,bw=b.w;
    this.x=ax*bw+aw*bx+ay*bz-az*by; this.y=ay*bw+aw*by+az*bx-ax*bz; this.z=az*bw+aw*bz+ax*by-ay*bx; this.w=aw*bw-ax*bx-ay*by-az*bz; return this._b(); }
  multiply(q){ return this.multiplyQuaternions(this,q); }
  premultiply(q){ return this.multiplyQuaternions(q,this); }
  setFromRotationMatrix(m){ const e=m.elements, m11=e[0],m12=e[4],m13=e[8],m21=e[1],m22=e[5],m23=e[9],m31=e[2],m32=e[6],m33=e[10], tr=m11+m22+m33; let s;
    if(tr>0){ s=.5/Math.sqrt(tr+1); this.w=.25/s; this.x=(m32-m23)*s; this.y=(m13-m31)*s; this.z=(m21-m12)*s; }
    else if(m11>m22&&m11>m33){ s=2*Math.sqrt(1+m11-m22-m33); this.w=(m32-m23)/s; this.x=.25*s; this.y=(m12+m21)/s; this.z=(m13+m31)/s; }
    else if(m22>m33){ s=2*Math.sqrt(1+m22-m11-m33); this.w=(m13-m31)/s; this.x=(m12+m21)/s; this.y=.25*s; this.z=(m23+m32)/s; }
    else { s=2*Math.sqrt(1+m33-m11-m22); this.w=(m21-m12)/s; this.x=(m13+m31)/s; this.y=(m23+m32)/s; this.z=.25*s; } return this._b(); }
}
class M4{
  constructor(){ this.elements=new Float32Array([1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]); }
  identity(){ this.elements.set([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]); return this; }
  copy(m){ this.elements.set(m.elements); return this; }
  clone(){ return new M4().copy(this); }
  compose(p,q,s){ const e=this.elements, x=q.x,y=q.y,z=q.z,w=q.w, x2=x+x,y2=y+y,z2=z+z, xx=x*x2,xy=x*y2,xz=x*z2,yy=y*y2,yz=y*z2,zz=z*z2,wx=w*x2,wy=w*y2,wz=w*z2, sx=s.x,sy=s.y,sz=s.z;
    e[0]=(1-(yy+zz))*sx; e[1]=(xy+wz)*sx; e[2]=(xz-wy)*sx; e[3]=0; e[4]=(xy-wz)*sy; e[5]=(1-(xx+zz))*sy; e[6]=(yz+wx)*sy; e[7]=0; e[8]=(xz+wy)*sz; e[9]=(yz-wx)*sz; e[10]=(1-(xx+yy))*sz; e[11]=0; e[12]=p.x; e[13]=p.y; e[14]=p.z; e[15]=1; return this; }
  multiplyMatrices(a,b){ const ae=a.elements, be=b.elements, te=this.elements; const r=new Float32Array(16);
    for(let i=0;i<4;i++) for(let j=0;j<4;j++){ r[j*4+i]=ae[i]*be[j*4]+ae[4+i]*be[j*4+1]+ae[8+i]*be[j*4+2]+ae[12+i]*be[j*4+3]; } te.set(r); return this; }
  multiply(m){ return this.multiplyMatrices(this,m); }
  premultiply(m){ return this.multiplyMatrices(m,this); }
  makeTranslation(x,y,z){ this.elements.set([1,0,0,0,0,1,0,0,0,0,1,0,x,y,z,1]); return this; }
  makeRotationZ(t){ const c=Math.cos(t),s=Math.sin(t); this.elements.set([c,s,0,0,-s,c,0,0,0,0,1,0,0,0,0,1]); return this; }
  makeRotationX(t){ const c=Math.cos(t),s=Math.sin(t); this.elements.set([1,0,0,0,0,c,s,0,0,-s,c,0,0,0,0,1]); return this; }
  makeRotationY(t){ const c=Math.cos(t),s=Math.sin(t); this.elements.set([c,0,-s,0,0,1,0,0,s,0,c,0,0,0,0,1]); return this; }
  makeScale(x,y,z){ this.elements.set([x,0,0,0,0,y,0,0,0,0,z,0,0,0,0,1]); return this; }
  normal3(){ // invers-transponierte 3x3 (für Normalen)
    const e=this.elements, a=e[0],b=e[1],c=e[2],d=e[4],f=e[5],g=e[6],h=e[8],i=e[9],j=e[10];
    const C00=f*j-i*g, C01=-(b*j-i*c), C02=b*g-f*c, C10=-(d*j-h*g), C11=a*j-h*c, C12=-(a*g-d*c), C20=d*i-h*f, C21=-(a*i-h*b), C22=a*f-d*b; const det=a*C00+d*C01+h*C02||1, id=1/det;
    return [C00*id,C01*id,C02*id, C10*id,C11*id,C12*id, C20*id,C21*id,C22*id]; }
}
/* ---------- Farbe (sRGB-Werte, PlayCanvas dekodiert im Shader) ---------- */
class Col{
  constructor(v){ this.r=1; this.g=1; this.b=1; this.cb=null; if(v!==undefined) this.set(v); }
  _c(){ if(this.cb) this.cb(); return this; }
  set(v){ if(v instanceof Col){ this.r=v.r; this.g=v.g; this.b=v.b; return this._c(); } if(typeof v==='string'){ if(v[0]==='#'){ let h=v.slice(1); if(h.length===3) h=h.split('').map(c=>c+c).join(''); return this.setHex(parseInt(h,16)); } return this; } return this.setHex(v); }
  setHex(h){ h=Math.floor(h); return this.setRGB((h>>16&255)/255,(h>>8&255)/255,(h&255)/255); }
  setRGB(r,g,b){ if(r===this.r&&g===this.g&&b===this.b) return this; this.r=r; this.g=g; this.b=b; return this._c(); }
  setHSL(h,s,l){ h=((h%1)+1)%1; s=Math.min(1,Math.max(0,s)); l=Math.min(1,Math.max(0,l)); if(s===0){ this.r=this.g=this.b=l; } else { const p=l<=.5?l*(1+s):l+s-l*s, q=2*l-p; const hue=(t)=>{ if(t<0) t+=1; if(t>1) t-=1; if(t<1/6) return q+(p-q)*6*t; if(t<.5) return p; if(t<2/3) return q+(p-q)*6*(2/3-t); return q; }; this.r=hue(h+1/3); this.g=hue(h); this.b=hue(h-1/3); } return this._c(); }
  getHex(){ const c=v=>Math.max(0,Math.min(255,Math.round(v*255))); return c(this.r)<<16|c(this.g)<<8|c(this.b); }
  copy(c){ this.r=c.r; this.g=c.g; this.b=c.b; return this._c(); }
  clone(){ const c=new Col(); c.r=this.r; c.g=this.g; c.b=this.b; return c; }
  lerp(c,t){ this.r+=(c.r-this.r)*t; this.g+=(c.g-this.g)*t; this.b+=(c.b-this.b)*t; return this._c(); }
  multiplyScalar(s){ this.r*=s; this.g*=s; this.b*=s; return this._c(); }
}
/* ---------- Geometrie ---------- */
class Attr{ constructor(arr,size){ this.array=arr; this.itemSize=size; this.count=arr.length/size; }
  getX(i){ return this.array[i*this.itemSize]; } getY(i){ return this.array[i*this.itemSize+1]; } getZ(i){ return this.array[i*this.itemSize+2]; }
  setX(i,v){ this.array[i*this.itemSize]=v; return this; } setY(i,v){ this.array[i*this.itemSize+1]=v; return this; } setZ(i,v){ this.array[i*this.itemSize+2]=v; return this; }
  setXY(i,x,y){ const k=i*this.itemSize; this.array[k]=x; this.array[k+1]=y; return this; }
  setXYZ(i,x,y,z){ const k=i*this.itemSize; this.array[k]=x; this.array[k+1]=y; this.array[k+2]=z; return this; } }
class Geo{
  constructor(){ this.attributes={}; this.index=null; this._pc=null; }
  setAttribute(n,a){ this.attributes[n]=a; this._pc=null; return this; }
  getAttribute(n){ return this.attributes[n]; }
  setIndex(i){ this.index=i==null?null:(i instanceof Attr?i:new Attr(new Uint32Array(i),1)); this._pc=null; return this; }
  applyMatrix4(m){ const p=this.attributes.position; if(p){ const e=m.elements, a=p.array; for(let i=0;i<a.length;i+=3){ const x=a[i],y=a[i+1],z=a[i+2]; a[i]=e[0]*x+e[4]*y+e[8]*z+e[12]; a[i+1]=e[1]*x+e[5]*y+e[9]*z+e[13]; a[i+2]=e[2]*x+e[6]*y+e[10]*z+e[14]; } }
    const n=this.attributes.normal; if(n){ const k=m.normal3(), a=n.array; for(let i=0;i<a.length;i+=3){ const x=a[i],y=a[i+1],z=a[i+2]; let nx=k[0]*x+k[1]*y+k[2]*z, ny=k[3]*x+k[4]*y+k[5]*z, nz=k[6]*x+k[7]*y+k[8]*z; const l=Math.hypot(nx,ny,nz)||1; a[i]=nx/l; a[i+1]=ny/l; a[i+2]=nz/l; } }
    this._pc=null; return this; }
  translate(x,y,z){ return this.applyMatrix4(new M4().makeTranslation(x,y,z)); }
  rotateX(t){ return this.applyMatrix4(new M4().makeRotationX(t)); }
  rotateY(t){ return this.applyMatrix4(new M4().makeRotationY(t)); }
  rotateZ(t){ return this.applyMatrix4(new M4().makeRotationZ(t)); }
  scale(x,y,z){ return this.applyMatrix4(new M4().makeScale(x,y,z)); }
  computeVertexNormals(){ const p=this.attributes.position.array, n=new Float32Array(p.length); const idx=this.index?this.index.array:null; const tri=idx?idx.length/3:p.length/9;
    for(let t=0;t<tri;t++){ const a=idx?idx[t*3]:t*3, b=idx?idx[t*3+1]:t*3+1, c=idx?idx[t*3+2]:t*3+2;
      const ux=p[b*3]-p[a*3], uy=p[b*3+1]-p[a*3+1], uz=p[b*3+2]-p[a*3+2], vx=p[c*3]-p[a*3], vy=p[c*3+1]-p[a*3+1], vz=p[c*3+2]-p[a*3+2];
      const nx=uy*vz-uz*vy, ny=uz*vx-ux*vz, nz=ux*vy-uy*vx; for(const k of [a,b,c]){ n[k*3]+=nx; n[k*3+1]+=ny; n[k*3+2]+=nz; } }
    for(let i=0;i<n.length;i+=3){ const l=Math.hypot(n[i],n[i+1],n[i+2])||1; n[i]/=l; n[i+1]/=l; n[i+2]/=l; }
    this.setAttribute('normal',new Attr(n,3)); return this; }
  toNonIndexed(){ if(!this.index) return this.clone(); const g=new Geo(), idx=this.index.array;
    for(const k in this.attributes){ const a=this.attributes[k], s=a.itemSize, out=new Float32Array(idx.length*s); for(let i=0;i<idx.length;i++) for(let j=0;j<s;j++) out[i*s+j]=a.array[idx[i]*s+j]; g.setAttribute(k,new Attr(out,s)); } return g; }
  clone(){ const g=new Geo(); for(const k in this.attributes){ const a=this.attributes[k]; g.setAttribute(k,new Attr(a.array.slice(),a.itemSize)); } if(this.index) g.index=new Attr(this.index.array.slice(),1); return g; }
  dispose(){} computeBoundingSphere(){}
  _mesh(prim){ if(this._pc&&this._pc.prim===prim) return this._pc.m;
    const p=this.attributes.position.array; if(!this.attributes.normal&&prim!==pc.PRIMITIVE_LINES) this.computeVertexNormals();
    const m=new pc.Mesh(device); m.setPositions(p); if(this.attributes.normal) m.setNormals(this.attributes.normal.array);
    const uv=this.attributes.uv; if(uv) m.setUvs(0,uv.array.length===p.length/3*2?uv.array:uv.array.slice(0,p.length/3*2));
    const c=this.attributes.color; if(c){ const n=p.length/3, s=c.itemSize, c8=new Uint8Array(n*4); for(let i=0;i<n;i++){ c8[i*4]=Math.max(0,Math.min(255,c.array[i*s]*255)); c8[i*4+1]=Math.max(0,Math.min(255,c.array[i*s+1]*255)); c8[i*4+2]=Math.max(0,Math.min(255,c.array[i*s+2]*255)); c8[i*4+3]=s>3?Math.max(0,Math.min(255,c.array[i*s+3]*255)):255; } m.setColors32(c8); }
    if(this.index) m.setIndices(p.length/3>65535?new Uint32Array(this.index.array):new Uint16Array(this.index.array));
    m.update(prim); this._pc={m,prim}; return m; }
}
const geoFrom=(pos,nor,uv,idx)=>{ const g=new Geo(); g.setAttribute('position',new Attr(new Float32Array(pos),3)); if(nor) g.setAttribute('normal',new Attr(new Float32Array(nor),3)); if(uv) g.setAttribute('uv',new Attr(new Float32Array(uv),2)); if(idx) g.index=new Attr(new Uint32Array(idx),1); return g; };
function BoxGeometry(w=1,h=1,d=1,ws=1,hs=1,ds=1){ const P=[],N=[],U=[],I=[]; let nv=0;
  const plane=(u,v,wA,udir,vdir,width,height,depth,gx,gy)=>{ const sw=width/gx, sh=height/gy, wh=width/2, hh=height/2, dh=depth/2; let c=0;
    for(let iy=0;iy<=gy;iy++){ const y=iy*sh-hh; for(let ix=0;ix<=gx;ix++){ const x=ix*sw-wh; const vec=[0,0,0]; vec[u]=x*udir; vec[v]=y*vdir; vec[wA]=dh; P.push(...vec); const nn=[0,0,0]; nn[wA]=depth>0?1:-1; N.push(...nn); U.push(ix/gx,1-iy/gy); c++; } }
    for(let iy=0;iy<gy;iy++) for(let ix=0;ix<gx;ix++){ const a=nv+ix+(gx+1)*iy, b=nv+ix+(gx+1)*(iy+1), cc=nv+ix+1+(gx+1)*(iy+1), dd=nv+ix+1+(gx+1)*iy; I.push(a,b,dd,b,cc,dd); } nv+=c; };
  plane(2,1,0,-1,-1,d,h,w,ds,hs); plane(2,1,0,1,-1,d,h,-w,ds,hs); plane(0,2,1,1,1,w,d,h,ws,ds); plane(0,2,1,1,-1,w,d,-h,ws,ds); plane(0,1,2,1,-1,w,h,d,ws,hs); plane(0,1,2,-1,-1,w,h,-d,ws,hs);
  return geoFrom(P,N,U,I); }
function PlaneGeometry(w=1,h=1,ws=1,hs=1){ const P=[],N=[],U=[],I=[]; const gx=Math.floor(ws), gy=Math.floor(hs), sw=w/gx, sh=h/gy;
  for(let iy=0;iy<=gy;iy++){ const y=iy*sh-h/2; for(let ix=0;ix<=gx;ix++){ const x=ix*sw-w/2; P.push(x,-y,0); N.push(0,0,1); U.push(ix/gx,1-iy/gy); } }
  for(let iy=0;iy<gy;iy++) for(let ix=0;ix<gx;ix++){ const a=ix+(gx+1)*iy, b=ix+(gx+1)*(iy+1), c=ix+1+(gx+1)*(iy+1), d=ix+1+(gx+1)*iy; I.push(a,b,d,b,c,d); }
  return geoFrom(P,N,U,I); }
function CylinderGeometry(rt=1,rb=1,h=1,rs=32,hs=1,open=false,t0=0,tl=Math.PI*2){ const P=[],N=[],U=[],I=[]; let idx=0; const grid=[], hh=h/2, slope=(rb-rt)/h;
  for(let y=0;y<=hs;y++){ const row=[], v=y/hs, r=v*(rb-rt)+rt; for(let x=0;x<=rs;x++){ const u=x/rs, th=u*tl+t0, s=Math.sin(th), c=Math.cos(th); P.push(r*s,-v*h+hh,r*c); const n=[s,slope,c], l=Math.hypot(...n); N.push(n[0]/l,n[1]/l,n[2]/l); U.push(u,1-v); row.push(idx++); } grid.push(row); }
  for(let x=0;x<rs;x++) for(let y=0;y<hs;y++){ const a=grid[y][x], b=grid[y+1][x], c=grid[y+1][x+1], d=grid[y][x+1]; I.push(a,b,d,b,c,d); }
  const cap=(top)=>{ const r=top?rt:rb, sign=top?1:-1, c0=idx; for(let x=1;x<=rs;x++){ P.push(0,hh*sign,0); N.push(0,sign,0); U.push(.5,.5); idx++; } const cE=idx;
    for(let x=0;x<=rs;x++){ const u=x/rs, th=u*tl+t0, c=Math.cos(th), s=Math.sin(th); P.push(r*s,hh*sign,r*c); N.push(0,sign,0); U.push(c*.5+.5,s*.5*sign+.5); idx++; }
    for(let x=0;x<rs;x++){ const c=c0+x, i=cE+x; if(top) I.push(i,i+1,c); else I.push(i+1,i,c); } };
  if(!open){ if(rt>0) cap(true); if(rb>0) cap(false); }
  return geoFrom(P,N,U,I); }
function ConeGeometry(r=1,h=1,rs=32,hs=1,open=false,t0=0,tl=Math.PI*2){ return CylinderGeometry(0,r,h,rs,hs,open,t0,tl); }
function SphereGeometry(r=1,ws=32,hs=16,p0=0,pl=Math.PI*2,t0=0,tl=Math.PI){ ws=Math.max(3,Math.floor(ws)); hs=Math.max(2,Math.floor(hs)); const P=[],N=[],U=[],I=[], grid=[]; let idx=0; const tEnd=Math.min(t0+tl,Math.PI);
  for(let iy=0;iy<=hs;iy++){ const row=[], v=iy/hs; let uo=0; if(iy===0&&t0===0) uo=.5/ws; else if(iy===hs&&tEnd===Math.PI) uo=-.5/ws;
    for(let ix=0;ix<=ws;ix++){ const u=ix/ws; const x=-r*Math.cos(p0+u*pl)*Math.sin(t0+v*tl), y=r*Math.cos(t0+v*tl), z=r*Math.sin(p0+u*pl)*Math.sin(t0+v*tl); P.push(x,y,z); const l=Math.hypot(x,y,z)||1; N.push(x/l,y/l,z/l); U.push(u+uo,1-v); row.push(idx++); } grid.push(row); }
  for(let iy=0;iy<hs;iy++) for(let ix=0;ix<ws;ix++){ const a=grid[iy][ix+1], b=grid[iy][ix], c=grid[iy+1][ix], d=grid[iy+1][ix+1]; if(iy!==0||t0>0) I.push(a,b,d); if(iy!==hs-1||tEnd<Math.PI) I.push(b,c,d); }
  return geoFrom(P,N,U,I); }
function CapsuleGeometry(r=1,len=1,capSeg=4,radSeg=8){ const prof=[]; // [radius, y, nr, ny, v]
  for(let i=0;i<=capSeg;i++){ const a=-Math.PI/2+i/capSeg*Math.PI/2; prof.push([Math.cos(a)*r,-len/2+Math.sin(a)*r,Math.cos(a),Math.sin(a)]); }
  for(let i=0;i<=capSeg;i++){ const a=i/capSeg*Math.PI/2; prof.push([Math.cos(a)*r,len/2+Math.sin(a)*r,Math.cos(a),Math.sin(a)]); }
  const P=[],N=[],U=[],I=[], rows=prof.length, tot=len+Math.PI*r;
  for(let j=0;j<rows;j++){ const [pr,py,nr,ny]=prof[j]; for(let i=0;i<=radSeg;i++){ const th=i/radSeg*Math.PI*2, s=Math.sin(th), c=Math.cos(th); P.push(pr*s,py,pr*c); N.push(nr*s,ny,nr*c); U.push(i/radSeg,(py+len/2+r)/tot); } }
  for(let j=0;j<rows-1;j++) for(let i=0;i<radSeg;i++){ const a=j*(radSeg+1)+i, b=a+radSeg+1; I.push(a,a+1,b,b,a+1,b+1); }
  return geoFrom(P,N,U,I); }
function TorusGeometry(r=1,tube=.4,rs=12,ts=48,arc=Math.PI*2){ const P=[],N=[],U=[],I=[];
  for(let j=0;j<=rs;j++) for(let i=0;i<=ts;i++){ const u=i/ts*arc, v=j/rs*Math.PI*2; const x=(r+tube*Math.cos(v))*Math.cos(u), y=(r+tube*Math.cos(v))*Math.sin(u), z=tube*Math.sin(v); P.push(x,y,z); const cx=r*Math.cos(u), cy=r*Math.sin(u); const nx=x-cx, ny=y-cy, nz=z, l=Math.hypot(nx,ny,nz)||1; N.push(nx/l,ny/l,nz/l); U.push(i/ts,j/rs); }
  for(let j=1;j<=rs;j++) for(let i=1;i<=ts;i++){ const a=(ts+1)*j+i-1, b=(ts+1)*(j-1)+i-1, c=(ts+1)*(j-1)+i, d=(ts+1)*j+i; I.push(a,b,d,b,c,d); }
  return geoFrom(P,N,U,I); }
function CircleGeometry(r=1,seg=32){ const P=[0,0,0],N=[0,0,1],U=[.5,.5],I=[]; for(let s=0;s<=seg;s++){ const a=s/seg*Math.PI*2; P.push(r*Math.cos(a),r*Math.sin(a),0); N.push(0,0,1); U.push(Math.cos(a)*.5+.5,Math.sin(a)*.5+.5); } for(let i=1;i<=seg;i++) I.push(i,i+1,0); return geoFrom(P,N,U,I); }
function RingGeometry(ri=.5,ro=1,seg=32){ const P=[],N=[],U=[],I=[]; for(let j=0;j<=1;j++){ const r=j?ro:ri; for(let i=0;i<=seg;i++){ const a=i/seg*Math.PI*2; P.push(r*Math.cos(a),r*Math.sin(a),0); N.push(0,0,1); U.push((r*Math.cos(a)/ro+1)/2,(r*Math.sin(a)/ro+1)/2); } } for(let i=0;i<seg;i++){ const a=i, b=i+seg+1; I.push(a,b,a+1,b,b+1,a+1); } return geoFrom(P,N,U,I); }
function Polyhedron(V,F,r,detail){ const out=[]; const vtx=i=>new V3(V[i*3],V[i*3+1],V[i*3+2]);
  for(let f=0;f<F.length;f+=3){ const a=vtx(F[f]), b=vtx(F[f+1]), c=vtx(F[f+2]); const cols=detail+1, v=[];
    for(let i=0;i<=cols;i++){ v[i]=[]; const aj=a.clone().lerp(c,i/cols), bj=b.clone().lerp(c,i/cols), rows=cols-i; for(let j=0;j<=rows;j++) v[i][j]=(j===0&&i===cols)?aj:aj.clone().lerp(bj,j/rows); }
    for(let i=0;i<cols;i++) for(let j=0;j<2*(cols-i)-1;j++){ const k=Math.floor(j/2); if(j%2===0) out.push(v[i][k+1],v[i+1][k],v[i][k]); else out.push(v[i][k+1],v[i+1][k+1],v[i+1][k]); } }
  const P=[]; for(let t=0;t<out.length;t+=3){ let [a,b,c]=out.slice(t,t+3).map(p=>p.clone().normalize().multiplyScalar(r)); const n=new V3().subVectors(b,a).cross(new V3().subVectors(c,a)); const m=a.clone().add(b).add(c); if(n.dot(m)<0) [b,c]=[c,b]; P.push(a.x,a.y,a.z,b.x,b.y,b.z,c.x,c.y,c.z); }
  const g=geoFrom(P); const U=new Float32Array(P.length/3*2); for(let i=0;i<P.length/3;i++){ U[i*2]=Math.atan2(P[i*3+2],-P[i*3])/(Math.PI*2)+.5; U[i*2+1]=Math.acos(Math.max(-1,Math.min(1,P[i*3+1]/r)))/Math.PI; } g.setAttribute('uv',new Attr(U,2)); g.computeVertexNormals(); return g; }
const PHI=(1+Math.sqrt(5))/2, RP=1/PHI;
function IcosahedronGeometry(r=1,d=0){ return Polyhedron([-1,PHI,0,1,PHI,0,-1,-PHI,0,1,-PHI,0,0,-1,PHI,0,1,PHI,0,-1,-PHI,0,1,-PHI,PHI,0,-1,PHI,0,1,-PHI,0,-1,-PHI,0,1],[0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1],r,d); }
function DodecahedronGeometry(r=1,d=0){ return Polyhedron([-1,-1,-1,-1,-1,1,-1,1,-1,-1,1,1,1,-1,-1,1,-1,1,1,1,-1,1,1,1,0,-RP,-PHI,0,-RP,PHI,0,RP,-PHI,0,RP,PHI,-RP,-PHI,0,-RP,PHI,0,RP,-PHI,0,RP,PHI,0,-PHI,0,-RP,PHI,0,-RP,-PHI,0,RP,PHI,0,RP],[3,11,7,3,7,15,3,15,13,7,19,17,7,17,6,7,6,15,17,4,8,17,8,10,17,10,6,8,0,16,8,16,2,8,2,10,0,12,1,0,1,18,0,18,16,6,10,2,6,2,13,6,13,15,2,16,18,2,18,3,2,3,13,18,1,9,18,9,11,18,11,3,4,14,12,4,12,0,4,0,8,11,9,5,11,5,19,11,19,7,19,5,14,19,14,4,19,4,17,1,12,14,1,14,5,1,5,9],r,d); }
/* Shape + Extrusion (mit Fase) */
class Shape{ constructor(){ this.c=[]; this.cur=null; }
  moveTo(x,y){ this.c.push(['m',x,y]); this.cur=[x,y]; return this; }
  lineTo(x,y){ this.c.push(['l',x,y]); this.cur=[x,y]; return this; }
  quadraticCurveTo(cx,cy,x,y){ this.c.push(['q',cx,cy,x,y,this.cur[0],this.cur[1]]); this.cur=[x,y]; return this; }
  absarc(x,y,r,a0,a1,cw){ this.c.push(['a',x,y,r,a0,a1,!!cw]); let e=a1; this.cur=[x+r*Math.cos(e),y+r*Math.sin(e)]; return this; }
  getPoints(div){ const pts=[]; const push=(x,y)=>{ const l=pts[pts.length-1]; if(!l||Math.abs(l[0]-x)>1e-6||Math.abs(l[1]-y)>1e-6) pts.push([x,y]); };
    for(const k of this.c){ if(k[0]==='m'||k[0]==='l') push(k[1],k[2]);
      else if(k[0]==='q'){ const [,cx,cy,x,y,sx,sy]=k; for(let i=1;i<=div;i++){ const t=i/div, u=1-t; push(u*u*sx+2*u*t*cx+t*t*x,u*u*sy+2*u*t*cy+t*t*y); } }
      else { const [,x,y,r,a0,a1,cw]=k; let d=a1-a0; const TA=Math.PI*2; while(d<0) d+=TA; while(d>TA) d-=TA; if(d<1e-9) d=TA; if(cw) d=d===TA?-TA:d-TA; const n=div*2; for(let i=0;i<=n;i++){ const a=a0+d*i/n; push(x+r*Math.cos(a),y+r*Math.sin(a)); } } }
    const f=pts[0], l=pts[pts.length-1]; if(pts.length>2&&Math.abs(f[0]-l[0])<1e-6&&Math.abs(f[1]-l[1])<1e-6) pts.pop(); return pts; } }
function earcut(pts){ const n=pts.length, V=[...Array(n).keys()], out=[]; const area=(a,b,c)=>(pts[b][0]-pts[a][0])*(pts[c][1]-pts[a][1])-(pts[b][1]-pts[a][1])*(pts[c][0]-pts[a][0]);
  const inside=(p,a,b,c)=>{ const d1=area(p,a,b), d2=area(p,b,c), d3=area(p,c,a); return d1>=0&&d2>=0&&d3>=0; };
  let guard=0; while(V.length>3&&guard++<n*n){ let cut=false; for(let i=0;i<V.length;i++){ const a=V[(i+V.length-1)%V.length], b=V[i], c=V[(i+1)%V.length]; if(area(a,b,c)<=1e-12) continue; let ok=true; for(const p of V){ if(p===a||p===b||p===c) continue; if(inside(p,a,b,c)){ ok=false; break; } } if(ok){ out.push(a,b,c); V.splice(i,1); cut=true; break; } } if(!cut){ out.push(V[0],V[1],V[2]); V.splice(1,1); } }
  if(V.length===3) out.push(V[0],V[1],V[2]); return out; }
function ExtrudeGeometry(shape,o){ const depth=o.depth??1, bev=o.bevelEnabled!==false, bt=bev?(o.bevelThickness??.2):0, bsz=bev?(o.bevelSize??.1):0, bseg=bev?(o.bevelSegments??3):0, cs=o.curveSegments??12;
  let pts=shape.getPoints(cs); let A=0; for(let i=0;i<pts.length;i++){ const a=pts[i], b=pts[(i+1)%pts.length]; A+=a[0]*b[1]-b[0]*a[1]; } if(A<0) pts.reverse();
  const n=pts.length; const bis=pts.map((p,i)=>{ const a=pts[(i+n-1)%n], b=pts[(i+1)%n]; let e1x=p[0]-a[0], e1y=p[1]-a[1], e2x=b[0]-p[0], e2y=b[1]-p[1]; const l1=Math.hypot(e1x,e1y)||1, l2=Math.hypot(e2x,e2y)||1; const n1=[e1y/l1,-e1x/l1], n2=[e2y/l2,-e2x/l2]; let bx=n1[0]+n2[0], by=n1[1]+n2[1]; const bl=Math.hypot(bx,by)||1; bx/=bl; by/=bl; const c=Math.max(.5,bx*n1[0]+by*n1[1]); return [bx/c,by/c]; });
  const layers=[]; for(let b=0;b<=bseg;b++){ const t=bseg?b/bseg:1; layers.push([-bt*Math.cos(t*Math.PI/2),bsz*Math.sin(t*Math.PI/2)]); } layers.push([depth,bsz]); for(let b=bseg;b>=0;b--){ const t=bseg?b/bseg:1; layers.push([depth+bt*Math.cos(t*Math.PI/2),bsz*Math.sin(t*Math.PI/2)]); }
  const ring=([z,o])=>pts.map((p,i)=>[p[0]+bis[i][0]*o,p[1]+bis[i][1]*o,z]); const R=layers.map(ring); const P=[];
  const tri=(a,b,c)=>P.push(...a,...b,...c);
  for(let l=0;l<R.length-1;l++){ const r0=R[l], r1=R[l+1]; if(Math.abs(layers[l][0]-layers[l+1][0])<1e-9&&Math.abs(layers[l][1]-layers[l+1][1])<1e-9) continue; for(let i=0;i<n;i++){ const j=(i+1)%n; tri(r0[i],r0[j],r1[j]); tri(r0[i],r1[j],r1[i]); } }
  const T=earcut(pts); const f=R[0], k=R[R.length-1]; for(let t=0;t<T.length;t+=3){ tri(f[T[t]],f[T[t+2]],f[T[t+1]]); tri(k[T[t]],k[T[t+1]],k[T[t+2]]); }
  const g=geoFrom(P); g.setAttribute('uv',new Attr(new Float32Array(P.length/3*2),2)); g.computeVertexNormals();
  // Orientierung prüfen (Normalen nach außen)
  return g; }
function BufferGeometry(){ return new Geo(); }
/* ---------- Texturen ---------- */
class Tex{ constructor(canvas,srgb){ this.isTex=true; this.image=canvas; this.offset=new V2(); this._off=[0,0]; this.users=new Set();
    this.pc=new pc.Texture(device,{width:canvas.width,height:canvas.height,format:srgb?pc.PIXELFORMAT_SRGBA8:pc.PIXELFORMAT_RGBA8,mipmaps:true,flipY:true,addressU:pc.ADDRESS_REPEAT,addressV:pc.ADDRESS_REPEAT,minFilter:pc.FILTER_LINEAR_MIPMAP_LINEAR,magFilter:pc.FILTER_LINEAR,anisotropy:X.MAXANI});
    this.pc.setSource(canvas); }
  set anisotropy(v){ this.pc.anisotropy=v; } set needsUpdate(v){ if(v) this.pc.upload(); } }
/* ---------- Materialien ---------- */
const DIRTY=new Set(); const ANIMTEX=new Set();
const DoubleSide=2, BackSide=1, FrontSide=0, NormalBlending=1, AdditiveBlending=2;
class Mat{
  constructor(o={},kind='std'){ this.kind=kind; this.o=o; const m=this.pc=new pc.StandardMaterial(); this.color=new Col(o.color??0xffffff); this.emissive=new Col(o.emissive??0); this._op=o.opacity??1; this.rotation=0;
    this.map=o.map||null; this.emissiveMap=o.emissiveMap||null; this._ei=o.emissiveIntensity??1; this.transparent=!!o.transparent; this.blending=o.blending||NormalBlending; this.wind=null;
    this.isMeshStandardMaterial=kind==='std'; this.isMeshPhysicalMaterial=!!o._phys;
    const d=()=>this.dirty(); this.color.cb=d; this.emissive.cb=d;
    for(const t of [this.map,this.emissiveMap]) if(t) t.users.add(this);
    this._setup(); this.apply(); }
  get opacity(){ return this._op; } set opacity(v){ if(v!==this._op){ this._op=v; this.dirty(); } }
  get emissiveIntensity(){ return this._ei; } set emissiveIntensity(v){ if(v!==this._ei){ this._ei=v; this.dirty(); } }
  dirty(){ DIRTY.add(this); }
  _setup(){ const o=this.o, m=this.pc;
    if(this.kind==='std'){ m.useMetalness=true; m.metalness=o.metalness??0; m.gloss=1-(o.roughness??1); if(o.normalMap){ m.normalMap=o.normalMap.pc; m.bumpiness=o.normalScale?o.normalScale.x:1; }
      if(o.glossMap){ m.glossMap=o.glossMap.pc; m.glossMapChannel='r'; m.gloss=1; }
      if(o.ormMap){ const t=o.ormMap.pc; m.aoMap=t; m.aoMapChannel='r'; m.glossMap=t; m.glossMapChannel='g'; m.glossInvert=true; m.gloss=1; m.metalnessMap=t; m.metalnessMapChannel='b'; m.metalness=1; }
      if(o.clearcoat){ m.clearCoat=o.clearcoat; m.clearCoatGloss=1-(o.clearcoatRoughness??0); } if(o.vertexColors) m.diffuseVertexColor=true; if(this.map) m.diffuseMap=this.map.pc; if(this.emissiveMap) m.emissiveMap=this.emissiveMap.pc; m.specularityFactor=o.specularity??1; }
    else { m.useLighting=false; m.useSkybox=false; m.diffuse.set(0,0,0); if(this.map) m.emissiveMap=this.map.pc; if(o.vertexColors) m.emissiveVertexColor=true; }
    const tmap=this.map; if(tmap&&(this.transparent||o.alphaTest)){ m.opacityMap=tmap.pc; m.opacityMapChannel='a'; }
    if(o.alphaTest){ m.alphaTest=o.alphaTest; if(!this.transparent){ m.blendType=pc.BLEND_NORMAL; m.depthWrite=true; } } // transparente Ebene: nicht im SSAO-Prepass
    if(this.transparent) m.blendType=this.blending===AdditiveBlending?pc.BLEND_ADDITIVEALPHA:pc.BLEND_NORMAL;
    if(o.side===DoubleSide||this.kind==='sprite'){ m.cull=pc.CULLFACE_NONE; m.twoSidedLighting=this.kind==='std'; } else if(o.side===BackSide) m.cull=pc.CULLFACE_FRONT;
    if(o.depthWrite===false) m.depthWrite=false;
    if(o.depthTest===false) m.depthTest=false;
    if(o.polygonOffset){ m.depthBias=-1; m.slopeDepthBias=o.polygonOffsetFactor||-1; }
    if(o.fog===false) m.useFog=false; }
  apply(){ const m=this.pc, c=this.color, e=this.emissive;
    if(this.kind==='std'){ m.diffuse.set(c.r,c.g,c.b); m.emissive.set(e.r,e.g,e.b); m.emissiveIntensity=this._ei; }
    else { m.emissive.set(c.r,c.g,c.b); m.emissiveIntensity=1; }
    m.opacity=this._op; if(this.map&&(this.map.offset.x||this.map.offset.y)){ const v=new pc.Vec2(this.map.offset.x,this.map.offset.y); if(this.kind==='std') m.diffuseMapOffset=v; else m.emissiveMapOffset=v; if(m.opacityMap) m.opacityMapOffset=v; }
    m.update(); }
}
const MeshStandardMaterial=function(o){ return new Mat(o,'std'); };
const MeshPhysicalMaterial=function(o){ return new Mat(Object.assign({_phys:true},o),'std'); };
const MeshBasicMaterial=function(o){ return new Mat(o,'basic'); };
const SpriteMaterial=function(o){ return new Mat(Object.assign({transparent:true},o),'sprite'); };
const LineBasicMaterial=function(o){ return new Mat(o,'basic'); };
function windify(mat,str,quad){ mat.wind={str,quad}; }
/* ---------- Szenenknoten ---------- */
let CAMNODE=null; const _q=new Q(), _qc=new pc.Quat();
class Node{
  constructor(entity){ this.e=entity||new pc.Entity(); this.position=new V3(); this.rotation=new Eu(); this.quaternion=new Q(); this.scale=new V3(1,1,1); this.children=[]; this.parent=null; this.visible=true; this.matrixWorld=new M4(); this.matrix=new M4(); this.userData={};
    this._ve=true; this._p=[0,0,0]; this._s=[1,1,1]; this._r=[0,0,0,'XYZ']; this._qv=0; this.isObject3D=true; }
  add(...cs){ for(const c of cs){ if(c.parent) c.parent.remove(c); c.parent=this; this.children.push(c); this.e.addChild(c.e); } return this; }
  remove(...cs){ for(const c of cs){ const i=this.children.indexOf(c); if(i>=0){ this.children.splice(i,1); c.parent=null; this.e.removeChild(c.e); } } return this; }
  traverse(fn){ fn(this); for(const c of this.children) c.traverse(fn); }
  _quat(){ // aktuelle lokale Rotation
    if(this.quaternion.v!==this._qv) return this.quaternion; const r=this.rotation; if(r.x!==this._r[0]||r.y!==this._r[1]||r.z!==this._r[2]||r.order!==this._r[3]){ const v=this.quaternion.v; this.quaternion._euler(r); this.quaternion.v=v; } return this.quaternion; }
  updateMatrix(){ this.matrix.compose(this.position,this._quat(),this.scale); return this; }
  updateWorld(){ if(this.external){ this.matrixWorld.elements.set(this.e.getWorldTransform().data); return this; } const q=this._quat(); this.matrixWorld.compose(this.position,q,this.scale); if(this.parent&&this.parent!==ROOT) { this.parent.updateWorld(); this.matrixWorld.premultiply(this.parent.matrixWorld); } return this; }
  getWorldPosition(v){ this.updateWorld(); const e=this.matrixWorld.elements; return v.set(e[12],e[13],e[14]); }
  lookAt(x,y,z){ const t=x instanceof V3||(x&&x.x!==undefined)?x:new V3(x,y,z); const p=this.position; let zx,zy,zz; if(this.isCamera){ zx=p.x-t.x; zy=p.y-t.y; zz=p.z-t.z; } else { zx=t.x-p.x; zy=t.y-p.y; zz=t.z-p.z; }
    let l=Math.hypot(zx,zy,zz)||1; zx/=l; zy/=l; zz/=l; let ux=0,uy=1,uz=0; if(Math.abs(zy)>.9999){ ux=0; uy=0; uz=1; }
    let xx=uy*zz-uz*zy, xy=uz*zx-ux*zz, xz=ux*zy-uy*zx; l=Math.hypot(xx,xy,xz)||1; xx/=l; xy/=l; xz/=l; const yx=zy*xz-zz*xy, yy=zz*xx-zx*xz, yz=zx*xy-zy*xx;
    const m=new M4(); m.elements.set([xx,xy,xz,0,yx,yy,yz,0,zx,zy,zz,0,0,0,0,1]); this.quaternion.setFromRotationMatrix(m);
    const e=new Eu(); // Euler aus Quaternion (XYZ) für spätere rotation.z-Zugriffe
    const q=this.quaternion, m11=1-2*(q.y*q.y+q.z*q.z), m12=2*(q.x*q.y-q.w*q.z), m13=2*(q.x*q.z+q.w*q.y), m22=1-2*(q.x*q.x+q.z*q.z), m23=2*(q.y*q.z-q.w*q.x), m32=2*(q.y*q.z+q.w*q.x), m33=1-2*(q.x*q.x+q.y*q.y);
    e.y=Math.asin(Math.max(-1,Math.min(1,m13))); if(Math.abs(m13)<.9999999){ e.x=Math.atan2(-m23,m33); e.z=Math.atan2(-m12,m11); } else { e.x=Math.atan2(m32,m22); e.z=0; }
    this.rotation.copy(e); this._r=[NaN,0,0,'XYZ']; this._qv=this.quaternion.v; return this; }
  _sync(){ if(this.external){ const ch=this.children; for(let i=0;i<ch.length;i++) ch[i]._sync(); return; } const vis=this.visible; if(vis!==this._ve){ this._ve=vis; this.e.enabled=vis; } if(!vis) return;
    const p=this.position, s=this.scale, P=this._p, S=this._s;
    if(p.x!==P[0]||p.y!==P[1]||p.z!==P[2]){ P[0]=p.x; P[1]=p.y; P[2]=p.z; this.e.setLocalPosition(p.x,p.y,p.z); }
    if(s.x!==S[0]||s.y!==S[1]||s.z!==S[2]){ S[0]=s.x; S[1]=s.y; S[2]=s.z; this.e.setLocalScale(s.x||1e-5,s.y||1e-5,s.z||1e-5); }
    const q=this.quaternion, r=this.rotation, Rr=this._r;
    if(q.v!==this._qv){ this._qv=q.v; this.e.setLocalRotation(_qc.set(q.x,q.y,q.z,q.w)); }
    else if(r.x!==Rr[0]||r.y!==Rr[1]||r.z!==Rr[2]||r.order!==Rr[3]){ Rr[0]=r.x; Rr[1]=r.y; Rr[2]=r.z; Rr[3]=r.order; const v=q.v; q._euler(r); q.v=v; this.e.setLocalRotation(_qc.set(q.x,q.y,q.z,q.w)); }
    if(this.isSprite) this._bill();
    const ch=this.children; for(let i=0;i<ch.length;i++) ch[i]._sync(); }
}
const Group=function(){ return new Node(); };
function attach(entity){ const n=new Node(entity); n.external=true; n.parent=null; ROOT.children.push(n); n.detach=()=>{ const i=ROOT.children.indexOf(n); if(i>=0) ROOT.children.splice(i,1); }; return n; }
const Object3D=function(){ return new Node(); };
class XMesh extends Node{
  constructor(geo,mat,prim){ super(); if(Array.isArray(mat)) mat=mat[4]||mat[0]; this.geometry=geo; this._mat=mat; this.isMesh=true;
    this.mi=new pc.MeshInstance(geo._mesh(prim??pc.PRIMITIVE_TRIANGLES),mat.pc); this.e.addComponent('render',{meshInstances:[this.mi],castShadows:false,receiveShadows:true}); this._cs=false; this._rs=true; }
  get material(){ return this._mat; } set material(m){ this._mat=m; this.mi.material=m.pc; }
  get castShadow(){ return this._cs; } set castShadow(v){ v=!!v; if(v!==this._cs){ this._cs=v; this.e.render.castShadows=v; } }
  get receiveShadow(){ return this._rs; } set receiveShadow(v){ v=!!v; if(v!==this._rs){ this._rs=v; this.e.render.receiveShadows=v; } }
  set frustumCulled(v){ this.mi.cull=!!v; } get frustumCulled(){ return this.mi.cull; }
  set renderOrder(v){ this._ro=v; } get renderOrder(){ return this._ro||0; }
  set matrixAutoUpdate(v){}
}
const Mesh=function(g,m){ return new XMesh(g,m); };
const LineSegments=function(g,m){ return new XMesh(g,m,pc.PRIMITIVE_LINES); };
let QUAD=null; const _bq=new pc.Quat(), _rq=new pc.Quat(), _ax=new pc.Vec3(0,0,1);
class XSprite extends XMesh{ constructor(mat){ if(!QUAD) QUAD=PlaneGeometry(1,1); super(QUAD,mat); this.isSprite=true; this.e.render.castShadows=false; this.e.render.receiveShadows=false; }
  _bill(){ if(!CAMNODE) return; const cq=CAMNODE.e.getRotation(); const r=this._mat.rotation||0; if(r){ _rq.setFromAxisAngle(_ax,r*DEG); _bq.mul2(cq,_rq); this.e.setRotation(_bq); } else this.e.setRotation(cq); } }
const Sprite=function(m){ return new XSprite(m); };

/* GPU-Skinning: starre Körperteile -> ein Mesh pro Figur (1 Draw Call statt ~12) */
const SKINS=new Map();
class XSkinned extends Node{
  constructor(key,parts,bones,root,mat){ super(); this.isMesh=true; this._mat=mat; let S=SKINS.get(key);
    if(!S){ const rel=bones.map(b=>{ let x=0,y=0,z=0,n=b; while(n&&n!==root){ x+=n.position.x; y+=n.position.y; z+=n.position.z; n=n.parent; } return [x,y,z]; });
      let nv=0; for(const [g] of parts) nv+=g.attributes.position.count;
      const P=new Float32Array(nv*3), N=new Float32Array(nv*3), U=new Float32Array(nv*2), C=new Uint8Array(nv*4), BI=new Uint8Array(nv*4), BW=new Float32Array(nv*4); let o=0;
      for(const [g,bi] of parts){ const p=g.attributes.position.array, n=g.attributes.normal?g.attributes.normal.array:null, uv=g.attributes.uv?g.attributes.uv.array:null, c=g.attributes.color, cnt=g.attributes.position.count, [rx,ry,rz]=rel[bi];
        for(let i=0;i<cnt;i++){ const k=o+i; P[k*3]=p[i*3]+rx; P[k*3+1]=p[i*3+1]+ry; P[k*3+2]=p[i*3+2]+rz; if(n){ N[k*3]=n[i*3]; N[k*3+1]=n[i*3+1]; N[k*3+2]=n[i*3+2]; } if(uv){ U[k*2]=uv[i*2]; U[k*2+1]=uv[i*2+1]; }
          if(c){ const s=c.itemSize; C[k*4]=Math.min(255,c.array[i*s]*255); C[k*4+1]=Math.min(255,c.array[i*s+1]*255); C[k*4+2]=Math.min(255,c.array[i*s+2]*255); } else { C[k*4]=C[k*4+1]=C[k*4+2]=255; } C[k*4+3]=255; BI[k*4]=bi; BW[k*4]=1; }
        o+=cnt; }
      const m=new pc.Mesh(device); m.setPositions(P); m.setNormals(N); m.setUvs(0,U); m.setColors32(C);
      m.setVertexStream(pc.SEMANTIC_BLENDINDICES,BI,4,nv,pc.TYPE_UINT8,false); m.setVertexStream(pc.SEMANTIC_BLENDWEIGHT,BW,4,nv,pc.TYPE_FLOAT32,false); m.update(pc.PRIMITIVE_TRIANGLES);
      const ibp=rel.map(([x,y,z])=>new pc.Mat4().setTranslate(-x,-y,-z)); S={mesh:m,skin:new pc.Skin(device,ibp,bones.map((b,i)=>'bone'+i))}; m.skin=S.skin; SKINS.set(key,S); }
    const si=new pc.SkinInstance(S.skin); si.bones=bones.map(b=>b.e); si.rootBone=root.e;
    this.mi=new pc.MeshInstance(S.mesh,mat.pc); this.mi.skinInstance=si; this.mi.setCustomAabb(new pc.BoundingBox(new pc.Vec3(0,1,0),new pc.Vec3(1.3,1.5,1.3)));
    this.e.addComponent('render',{meshInstances:[this.mi],castShadows:false,receiveShadows:true}); this._cs=false; this._rs=true; }
  get material(){ return this._mat; }
  get castShadow(){ return this._cs; } set castShadow(v){ v=!!v; if(v!==this._cs){ this._cs=v; this.e.render.castShadows=v; } }
  get receiveShadow(){ return this._rs; } set receiveShadow(v){ v=!!v; if(v!==this._rs){ this._rs=v; this.e.render.receiveShadows=v; } }
}
const Skinned=function(k,p,b,r,m){ return new XSkinned(k,p,b,r,m); };
/* Instancing mit Farbe pro Instanz und optionalem Wind */
const IMATS=new Map();
function instMat(mat){ let m=IMATS.get(mat); if(m) return m; m=mat.pc.clone(); m.shaderChunks.version='2.9'; const ck=m.getShaderChunks(pc.SHADERLANGUAGE_GLSL), base=pc.ShaderChunks.get(device,pc.SHADERLANGUAGE_GLSL);
  ck.set('transformInstancingVS',`attribute vec4 instance_line1;\nattribute vec4 instance_line2;\nattribute vec4 instance_line3;\nattribute vec4 instance_line4;\nattribute vec4 instance_color;\nvarying vec3 vInstCol;\nmat4 getModelMatrix(){ vInstCol=instance_color.rgb; return matrix_model*mat4(instance_line1,instance_line2,instance_line3,instance_line4); }\n`);
  ck.set('diffusePS',base.get('diffusePS').replace('void getAlbedo() {','varying vec3 vInstCol;\nvoid getAlbedo() {').replace('dAlbedo = material_diffuse.rgb;','dAlbedo = material_diffuse.rgb * gammaCorrectInput(saturate(vInstCol));'));
  if(mat.wind){ const w=mat.wind; ck.set('transformVS',base.get('transformVS').replace('vec4 posW = modelMatrix * vec4(localPos, 1.0);',`vec4 posW = modelMatrix * vec4(localPos, 1.0);
    { vec3 ip=modelMatrix[3].xyz; float sc=length(modelMatrix[1].xyz); vec3 w3=vec3(.86,0.,.5); float ph=dot(ip.xz,w3.xz)*.09; float gust=.55+.45*sin(uWindT*.37-ph*.6);
      float wv=(sin(uWindT*1.35-ph)*.6+sin(uWindT*2.9-ph*1.7+ip.x*.3+ip.z*.2)*.22)*gust+.3*gust; float hh=max(localPos.y,0.); posW.xyz+=w3*(${w.quad?'hh*hh':'hh'})*${w.str.toFixed(4)}*wv*sc*sc; }`).replace('vec4 evalWorldPosition','uniform float uWindT;\nvec4 evalWorldPosition')); }
  m.setAttribute('instance_line1',pc.SEMANTIC_ATTR12); m.setAttribute('instance_line2',pc.SEMANTIC_ATTR13); m.setAttribute('instance_line3',pc.SEMANTIC_ATTR14); m.setAttribute('instance_line4',pc.SEMANTIC_ATTR15); m.setAttribute('instance_color',pc.SEMANTIC_ATTR11);
  m.update(); IMATS.set(mat,m); return m; }
let IFMT=null; const INST=new Set();
class XInst extends XMesh{
  constructor(geo,mat,count){ super(geo,mat); this.count=count; this.buf=new Float32Array(count*20); for(let i=0;i<count;i++){ const o=i*20; this.buf[o]=this.buf[o+5]=this.buf[o+10]=this.buf[o+15]=1; this.buf[o+16]=this.buf[o+17]=this.buf[o+18]=this.buf[o+19]=1; }
    if(!IFMT) IFMT=new pc.VertexFormat(device,[{semantic:pc.SEMANTIC_ATTR12,components:4,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_ATTR13,components:4,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_ATTR14,components:4,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_ATTR15,components:4,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_ATTR11,components:4,type:pc.TYPE_FLOAT32}]);
    this.vb=new pc.VertexBuffer(device,IFMT,count,{usage:pc.BUFFER_DYNAMIC,data:this.buf}); this.mi.material=instMat(mat); this.mi.setInstancing(this.vb); this.mi.cull=false;
    const self=this; this.instanceMatrix={ set needsUpdate(v){ if(v) INST.add(self); } }; this.instanceColor={ set needsUpdate(v){ if(v) INST.add(self); } }; INST.add(this); }
  set material(m){ this._mat=m; this.mi.material=instMat(m); } get material(){ return this._mat; }
  setMatrixAt(i,m){ this.buf.set(m.elements,i*20); }
  setColorAt(i,c){ const o=i*20+16; this.buf[o]=c.r; this.buf[o+1]=c.g; this.buf[o+2]=c.b; }
  _upload(){ this.vb.setData(this.buf); }
}
const InstancedMesh=function(g,m,n){ return new XInst(g,m,n); };
/* Partikel: ein dynamisches Billboard-Mesh pro Pool */
class Particles{
  constructor(n,tex,additive){ this.n=n; this.p=[]; for(let i=0;i<n;i++) this.p.push({on:false,x:0,y:0,z:0,s:1,rot:0,r:1,g:1,b:1,a:1}); this.i=0;
    const fmt=new pc.VertexFormat(device,[{semantic:pc.SEMANTIC_POSITION,components:3,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_NORMAL,components:3,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_TEXCOORD0,components:2,type:pc.TYPE_FLOAT32},{semantic:pc.SEMANTIC_COLOR,components:4,type:pc.TYPE_UINT8,normalize:true}]);
    this.stride=36; this.ab=new ArrayBuffer(n*4*36); this.f=new Float32Array(this.ab); this.u8=new Uint8Array(this.ab);
    for(let i=0;i<n*4;i++){ const k=i*9; this.f[k+3]=0; this.f[k+4]=0; this.f[k+5]=1; const c=i%4; this.f[k+6]=(c===1||c===2)?1:0; this.f[k+7]=(c>=2)?1:0; }
    this.vb=new pc.VertexBuffer(device,fmt,n*4,{usage:pc.BUFFER_DYNAMIC,data:this.ab});
    const idx=new Uint16Array(n*6); for(let i=0;i<n;i++){ idx.set([i*4,i*4+1,i*4+2,i*4,i*4+2,i*4+3],i*6); } this.ib=new pc.IndexBuffer(device,pc.INDEXFORMAT_UINT16,n*6,pc.BUFFER_STATIC,idx.buffer);
    const mesh=new pc.Mesh(device); mesh.vertexBuffer=this.vb; mesh.indexBuffer[0]=this.ib; mesh.primitive[0].type=pc.PRIMITIVE_TRIANGLES; mesh.primitive[0].base=0; mesh.primitive[0].count=0; mesh.primitive[0].indexed=true; this.mesh=mesh;
    const m=new pc.StandardMaterial(); m.useLighting=false; m.useSkybox=false; m.diffuse.set(0,0,0); m.emissive.set(1,1,1); m.emissiveMap=tex.pc; m.emissiveVertexColor=true; m.opacityMap=tex.pc; m.opacityMapChannel='a'; m.opacityVertexColor=true; m.opacityVertexColorChannel='a'; m.blendType=additive?pc.BLEND_ADDITIVEALPHA:pc.BLEND_NORMAL; m.depthWrite=false; m.cull=pc.CULLFACE_NONE; m.update();
    this.mi=new pc.MeshInstance(mesh,m); this.mi.cull=false; this.e=new pc.Entity(); this.e.addComponent('render',{meshInstances:[this.mi],castShadows:false,receiveShadows:false}); app.root.addChild(this.e); }
  write(cx,cy,cz){ if(!CAMNODE) return; const cq=CAMNODE.quaternion; const rx=new V3(1,0,0).applyQuaternion(cq), uy=new V3(0,1,0).applyQuaternion(cq); const f=this.f, u8=this.u8; let k=0;
    const list=[]; for(const p of this.p) if(p.on&&p.a>.003) list.push(p); if(!this.add) list.sort((a,b)=>((b.x-cx)**2+(b.y-cy)**2+(b.z-cz)**2)-((a.x-cx)**2+(a.y-cy)**2+(a.z-cz)**2));
    for(const p of list){ const c=Math.cos(p.rot)*p.s*.5, s=Math.sin(p.rot)*p.s*.5; const ax=rx.x*c+uy.x*s, ay=rx.y*c+uy.y*s, az=rx.z*c+uy.z*s, bx=-rx.x*s+uy.x*c, by=-rx.y*s+uy.y*c, bz=-rx.z*s+uy.z*c;
      const corners=[[-1,-1],[1,-1],[1,1],[-1,1]]; for(let j=0;j<4;j++){ const [i1,i2]=corners[j], o=(k*4+j)*9; f[o]=p.x+ax*i1+bx*i2; f[o+1]=p.y+ay*i1+by*i2; f[o+2]=p.z+az*i1+bz*i2; const bo=o*4+32; u8[bo]=p.r*255; u8[bo+1]=p.g*255; u8[bo+2]=p.b*255; u8[bo+3]=Math.min(1,p.a)*255; } k++; }
    this.vb.setData(this.ab); this.mesh.primitive[0].count=k*6; this.e.enabled=k>0; }
}
/* ---------- Licht ---------- */
class XPoint extends Node{ constructor(color,int,dist){ super(); const c=new Col(color); this.e.addComponent('light',{type:'omni',color:new pc.Color(c.r,c.g,c.b),intensity:0,range:dist||10,castShadows:false,falloffMode:pc.LIGHTFALLOFF_INVERSESQUARED}); this._i=-1; this.intensity=int||0; this.distance=dist||10; }
  get intensity(){ return this._i; } set intensity(v){ if(v===this._i) return; this._i=v; const k=Math.min(10,v/4); this.e.light.intensity=k; const on=k>.01; if(this.e.light.enabled!==on) this.e.light.enabled=on; }
  get distance(){ return this._d; } set distance(v){ if(v!==this._d){ this._d=v; this.e.light.range=v; } } }
const PointLight=function(c,i,d){ return new XPoint(c,i,d); };
/* ---------- Kamera ---------- */
class XCam extends Node{ constructor(fov,near,far){ super(); this.isCamera=true; this.fov=fov; this.near=near; this.far=far; this.aspect=1; this.e.addComponent('camera',{fov,nearClip:near,farClip:far,clearColor:new pc.Color(.66,.6,.5)}); }
  updateProjectionMatrix(){ this.e.camera.fov=this.fov; }
  getWorldDirection(v){ this.updateWorld(); const e=this.matrixWorld.elements; return v.set(-e[8],-e[9],-e[10]).normalize(); } }
/* ---------- Wurzel & Takt ---------- */
let ROOT=null;
function init(a){ app=a; device=a.graphicsDevice; X.device=device; ROOT=new Node(a.root); ROOT.e=a.root; X.scene=ROOT; X.MAXANI=Math.min(8,device.maxAnisotropy||1); device.scope.resolve('uWindT').setValue(0); }
function setCamera(c){ CAMNODE=c; }
const _wt=[];
function sync(time){ for(const m of DIRTY) m.apply(); DIRTY.clear(); for(const i of INST) i._upload(); INST.clear(); device.scope.resolve('uWindT').setValue(time||0);
  for(const c of ROOT.children) c._sync(); }
function loadTex(url,srgb){ return new Promise((res,rej)=>{ const im=new Image(); im.onload=()=>res(new Tex(im,srgb)); im.onerror=()=>rej(new Error('Bild fehlt: '+url)); im.src=url; }); }
return { init, setCamera, sync, attach, loadTex, V2, V3, Eu, Q, M4, Col, Attr, Geo, BoxGeometry, PlaneGeometry, CylinderGeometry, ConeGeometry, SphereGeometry, CapsuleGeometry, TorusGeometry, CircleGeometry, RingGeometry, IcosahedronGeometry, DodecahedronGeometry, Shape, ExtrudeGeometry, BufferGeometry, BufferAttribute:function(a,s){ return new Attr(a,s); }, Float32BufferAttribute:function(a,s){ return new Attr(a instanceof Float32Array?a:new Float32Array(a),s); },
  Tex, Mat, MeshStandardMaterial, MeshPhysicalMaterial, MeshBasicMaterial, SpriteMaterial, LineBasicMaterial, windify, Node, Group, Object3D, Mesh, LineSegments, Sprite, Skinned, InstancedMesh, Particles, PointLight, XCam,
  DoubleSide, BackSide, FrontSide, NormalBlending, AdditiveBlending, MAXANI:1, scene:null, device:null };
})();
