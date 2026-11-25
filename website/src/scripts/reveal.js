var e = new IntersectionObserver(
  (t) => {
    for (let n of t) n.isIntersecting && (n.target.classList.add(`in`), e.unobserve(n.target));
  },
  { rootMargin: `0px 0px -8% 0px` },
);
document.querySelectorAll(`.reveal`).forEach((t) => e.observe(t));
