window.addEventListener('scroll', () => {
    const hero = document.querySelector('.hero');
    if (hero) {
        hero.style.backgroundPositionY = `${window.scrollY * 0.3}px`;
    }
});

// Move shapes on scroll
window.addEventListener('scroll', () => {
    const scrollY = window.scrollY;
    document.querySelectorAll('.shape').forEach((el, i) => {
        const direction = i % 2 === 0 ? 1 : -1;
        el.style.transform = `translateY(${direction * scrollY * 0.05}px)`;
    });
});

//Animated stats counter
const statsSection = document.querySelector('.stats');
const stats = document.querySelectorAll('.stat-number');
let statsViewed = false;

function animateStats() {
    stats.forEach(el => {
        const target = +el.dataset.target;
        const duration = 2000;
        let start = 0;
        const stepTime = Math.max(Math.floor(duration / target), 1);
        const timer = setInterval(() => {
            start++;
            el.textContent = start.toLocaleString();
            if (start >= target) {
                clearInterval(timer);
                el.textContent = target.toLocaleString() + '+';
            }
        }, stepTime);
    });
}

if (statsSection) {
    new IntersectionObserver(entries => {
        if (entries[0].isIntersecting && !statsViewed) {
            animateStats();
            statsViewed = true;
        }
    }, { threshold: 0.5 }).observe(statsSection);
}
