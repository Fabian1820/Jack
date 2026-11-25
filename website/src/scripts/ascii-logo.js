var e = { x: 512, y: 512 },
  t = { x: 512, y: 525 },
  n = 600,
  r = [
    { node: { x: 512, y: 290 }, armR: 21, nodeR: 50, back: !0, offset: 0.12 },
    { node: { x: 704, y: 623 }, armR: 21, nodeR: 50, back: !0, offset: 0.45 },
    { node: { x: 320, y: 623 }, armR: 21, nodeR: 50, back: !0, offset: 0.78 },
    { node: { x: 718, y: 393 }, armR: 29, nodeR: 66, back: !1, offset: 0.29 },
    { node: { x: 306, y: 393 }, armR: 29, nodeR: 66, back: !1, offset: 0.62 },
    { node: { x: 512, y: 740 }, armR: 29, nodeR: 0, back: !1, offset: 0.95 },
  ],
  i = 74,
  a = { x: 512, y: 740 },
  o = 72,
  s = ` .·:;=+*x#%@`,
  c = `<>/\\{}[]=+*#01$%&?!;~^`,
  l = 0.26,
  u = (e) => (e < 0 ? 0 : e > 1 ? 1 : e),
  d = (e) => e - Math.floor(e),
  f = (e, t) => d(Math.sin(e * 127.1 + t * 311.7) * 43758.5453),
  p = (e) => e * e * (3 - 2 * e),
  m = (e) => (e < 0.5 ? 4 * e * e * e : 1 - (-2 * e + 2) ** 3 / 2);
function ee(e, t) {
  let n = Math.floor(e),
    r = Math.floor(t),
    i = p(e - n),
    a = p(t - r),
    o = f(n, r),
    s = f(n + 1, r),
    c = f(n, r + 1),
    l = f(n + 1, r + 1);
  return o + (s - o) * i + (c - o) * a + (o - s - c + l) * i * a;
}
function te(e, t, n, r, i) {
  let a = e - n.x,
    o = t - n.y,
    s = r.x - n.x,
    c = r.y - n.y,
    l = u((a * s + o * c) / (s * s + c * c)),
    d = a - s * l,
    f = o - c * l;
  return Math.sqrt(d * d + f * f) - i;
}
function h(e, t, n, r) {
  let i = e - n.x,
    a = t - n.y;
  return Math.sqrt(i * i + a * a) - r;
}
var g = (e, t, n) => [e[0] + (t[0] - e[0]) * n, e[1] + (t[1] - e[1]) * n, e[2] + (t[2] - e[2]) * n],
  ne = [58, 64, 84],
  re = [244, 246, 252],
  ie = [255, 84, 28],
  ae = [255, 184, 100],
  oe = [110, 214, 255],
  se = [184, 144, 255];
function _(p, _) {
  let v = p.getContext(`2d`, { alpha: !0 }),
    y = window.matchMedia(`(prefers-reduced-motion: reduce)`).matches,
    b = new Map(),
    x = 0,
    S = 0,
    C = 12,
    w = 7,
    T = 14,
    E = 0,
    D = 0,
    O = 1,
    k = { x: -9999, y: -9999, sx: -9999, sy: -9999, hover: 0, target: 0 },
    A = { x: 0, y: 0 },
    j = [],
    M = !1,
    N = !0,
    P = 0,
    F = performance.now(),
    I = 0,
    L = () => document.documentElement.dataset.light === `on`,
    R = () => (performance.now() - F) / 1e3;
  function z() {
    let e = p.getBoundingClientRect(),
      t = Math.min(window.devicePixelRatio || 1, 2);
    (x = e.width),
      (S = e.height),
      (p.width = Math.round(x * t)),
      (p.height = Math.round(S * t)),
      v.setTransform(t, 0, 0, t, 0, 0),
      (C = Math.max(8, Math.min(13, x / 64))),
      (v.font = `500 ${C}px "Geist Mono", ui-monospace, Menlo, monospace`),
      (v.textAlign = `center`),
      (v.textBaseline = `middle`),
      (w = v.measureText(`M`).width || C * 0.6),
      (T = C * 1.18),
      (E = Math.ceil(x / w)),
      (D = Math.ceil(S / T)),
      (O = (Math.min(x, S * 1.05) * 0.84) / n);
  }
  function ce(e) {
    let t = e[0] >> 3,
      n = e[1] >> 3,
      r = e[2] >> 3,
      i = (t << 10) | (n << 5) | r,
      a = b.get(i);
    return a || ((a = `rgb(${t << 3},${n << 3},${r << 3})`), b.set(i, a)), a;
  }
  function B(n) {
    v.clearRect(0, 0, x, S),
      (k.hover += (k.target - k.hover) * 0.08),
      (k.sx += (k.x - k.sx) * 0.22),
      (k.sy += (k.y - k.sy) * 0.22);
    let p = k.target ? u(k.x / x) * 2 - 1 : Math.sin(n * 0.5) * 0.55,
      _ = k.target ? u(k.y / S) * 2 - 1 : Math.cos(n * 0.37) * 0.4;
    (A.x += (p - A.x) * 0.05), (A.y += (_ - A.y) * 0.05);
    let b = Math.sin(n * 0.9) * 4,
      C = { x: A.x * 16 * 0.9, y: A.y * 16 * 0.9 + b * 0.6 },
      M = { x: -A.x * 16 * 0.45, y: -A.y * 16 * 0.45 + b },
      N = Math.min(x, S) * 0.3,
      P = N * N,
      F = (w / O) * 0.95,
      I = y ? 2 : u((n - 0.05) / 1.5),
      L = I * (D + 6),
      R = r.map((t) => {
        let r = d(n * l + t.offset),
          i = r > 0.3 ? m((r - 0.3) / 0.7) : -1,
          a = r <= 0.3 ? r / 0.3 : -1;
        return {
          arm: t,
          px: i >= 0 ? e.x + (t.node.x - e.x) * i : 0,
          py: i >= 0 ? e.y + (t.node.y - e.y) * i : 0,
          travel: i,
          ring: a,
        };
      }),
      z = R.reduce((e, t) => Math.max(e, t.travel >= 0 && t.travel < 0.15 ? 1 - t.travel / 0.15 : 0), 0);
    for (let e = j.length - 1; e >= 0; e--) n - j[e].t0 > 1.6 && j.splice(e, 1);
    let B = Math.floor(n * 14),
      V = ``;
    for (let l = 0; l < D && !(l > L); l++) {
      let d = l * T + T / 2,
        p = L - l < 3 && I < 1;
      for (let m = 0; m < E; m++) {
        let _ = m * w + w / 2,
          y = _,
          b = d,
          T = _ - k.sx,
          E = d - k.sy,
          D = T * T + E * E,
          A = 0;
        if (k.hover > 0.01 && D < P) {
          let e = 1 - Math.sqrt(D) / N;
          A = e * e * k.hover;
          let t = 0.45 * A;
          (y = _ - T * t), (b = d - E * t);
        }
        let I = 0;
        for (let e of j) {
          let t = n - e.t0,
            r = _ - e.x,
            i = d - e.y,
            a = Math.sqrt(r * r + i * i) || 1,
            o = 1 - Math.abs(a - t * 560) / 42;
          if (o > 0) {
            let e = o * (1 - t / 1.6);
            (I += e), (y -= (r / a) * e * 14), (b -= (i / a) * e * 14);
          }
        }
        let L = t.x + (y - M.x - x / 2) / O,
          H = t.y + (b - M.y - S / 2) / O,
          U = t.x + (y - C.x - x / 2) / O,
          W = t.y + (b - C.y - S / 2) / O,
          G = h(L, H, e, i),
          K = 1e9;
        for (let t of r)
          t.back
            ? (K = Math.min(K, te(U, W, e, t.node, t.armR), h(U, W, t.node, t.nodeR)))
            : ((G = Math.min(G, te(L, H, e, t.node, t.armR))),
              t.nodeR && (G = Math.min(G, h(L, H, t.node, t.nodeR))));
        let le = h(L, H, a, o),
          ue = u(0.5 - K / F),
          q = u(0.5 - G / F),
          J = u(0.5 - le / F),
          Y = Math.max(J, q, ue),
          X = ue * (0.42 + 0.14 * u(-K / 30)),
          de = 1 - 0.32 * u((H - 330) / 430),
          fe = 0.72 + 0.28 * u(-G / 26);
        X = X * (1 - q) + q * de * fe;
        let Z = J;
        (X = X * (1 - J) + J * (0.84 + 0.12 * u(-le / 40))),
          (X += q * z * 0.25 * u(1 - h(L, H, e, 0) / 140)),
          (X += (ee(m * 0.17, l * 0.29 - n * 0.7) - 0.5) * 0.2 * Y);
        for (let e of R) {
          let t = e.arm.back ? U : L,
            n = e.arm.back ? W : H,
            r = e.arm.back ? 0.55 : 1;
          if (e.travel >= 0) {
            let i = t - e.px,
              a = n - e.py,
              o = Math.exp(-(i * i + a * a) / 900);
            o > 0.01 && ((X += o * 0.55 * r), (Z = Math.max(Z, o * 0.95 * r)));
          } else if (e.ring >= 0) {
            let i = (e.arm.nodeR || o) + e.ring * 110,
              a = u(1 - Math.abs(h(t, n, e.arm.node, i)) / 14) * (1 - e.ring);
            a > 0 && ((X = Math.max(X, a * 0.42 * r)), (Z = Math.max(Z, a * 0.8)));
          }
        }
        if (Y < 0.05) {
          let e = ee(m * 0.07 + n * 0.06, l * 0.12 - n * 0.09);
          e > 0.62 && (X = Math.max(X, (e - 0.62) * 0.55));
        }
        if (((X += I * 0.5), (X += A * (Y > 0.05 ? 0.25 : 0.32)), X < 0.04 && !p)) continue;
        let pe = Math.round(u(X) * 11),
          Q = s[pe],
          me = A > 0.12 ? (A - 0.12) / 0.88 : 0;
        if (
          (((me > 0 && f(m * 1.3 + B, l * 0.7) < me * 0.75) || (I > 0.3 && f(m, l + B) < 0.5)) &&
            ((Q = c[Math.floor(f(m + B * 0.37, l) * 22)]), (pe = Math.max(pe, 4))),
          p && ((Q = c[Math.floor(f(m, l + B) * 22)]), (X = Math.max(X, 0.5))),
          Q === ` `)
        )
          continue;
        let he = u(X),
          $ = g(ne, re, he ** 0.85);
        Z > 0.01 && ($ = g($, g(ie, ae, u(he * 1.2 - 0.2)), u(Z)));
        let ge = u(A * 1.6 + I * 0.6);
        if (ge > 0.01) {
          let e = Math.atan2(E, T) / Math.PI,
            t = g(oe, se, (Math.sin(e * 3 + n * 2) + 1) / 2);
          $ = g($, g(t, ae, Z * 0.6), ge * 0.85);
        }
        p && ($ = g($, ae, 0.6));
        let _e = ce($);
        _e !== V && ((v.fillStyle = _e), (V = _e)), v.fillText(Q, _, d);
      }
    }
  }
  function V() {
    M && (B(R()), (P = requestAnimationFrame(V)));
  }
  function H() {
    let e = N && !y && !L() && !document.hidden;
    e && !M
      ? ((M = !0), (P = requestAnimationFrame(V)))
      : !e && M && ((M = !1), cancelAnimationFrame(P), (I = R()), B(I));
  }
  function U(e) {
    let t = p.getBoundingClientRect();
    (k.x = e.clientX - t.left),
      (k.y = e.clientY - t.top),
      k.sx < -999 && ((k.sx = k.x), (k.sy = k.y)),
      (k.target = 1),
      M || B(I || R());
  }
  _.addEventListener(`pointermove`, U),
    _.addEventListener(`pointerleave`, () => {
      k.target = 0;
    }),
    _.addEventListener(`pointerdown`, (e) => {
      let t = p.getBoundingClientRect(),
        n = e.clientX - t.left,
        r = e.clientY - t.top;
      n < -40 || r < -40 || n > x + 40 || r > S + 40 || j.push({ x: n, y: r, t0: R() });
    }),
    new IntersectionObserver(([e]) => {
      (N = e.isIntersecting), H();
    }).observe(p),
    new MutationObserver(H).observe(document.documentElement, {
      attributes: !0,
      attributeFilter: [`data-light`],
    }),
    document.addEventListener(`visibilitychange`, H),
    new ResizeObserver(() => {
      z(), M || B(I || R());
    }).observe(p);
  let W = () => {
    z(), y && B(10), H();
  };
  document.fonts?.ready ? document.fonts.ready.then(W) : W();
}
_(document.getElementById(`ascii`), document.getElementById(`hero`));
var v = document.querySelector(`.cmd`),
  y = v.querySelector(`.copy-state`);
v.addEventListener(`click`, async () => {
  try {
    await navigator.clipboard.writeText(v.dataset.copy),
      v.classList.add(`copied`),
      (y.textContent = `copiado ✓`),
      setTimeout(() => {
        v.classList.remove(`copied`), (y.textContent = `copiar`);
      }, 1600);
  } catch {
    y.textContent = `selecciona y copia`;
  }
});
