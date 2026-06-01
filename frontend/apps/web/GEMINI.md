# Nexus Design System (Premium Engineering Aesthetic)

All UI development in Nexus must strictly adhere to this design system to ensure a unified "Engineering Intelligence" aesthetic (inspired by Linear, Vercel, and GitHub).

## Color System

- **Primary Background**: `#0F172A` (Slate 950)
- **Secondary/Card Background**: `#1E293B` (Slate 800)
- **Accent Color**: `#06B6D4` (Cyan 500) - Used for primary actions, active states, and focus rings.
- **Surface/Light**: `#F8FAFC` (Slate 50) - Used sparingly for high-contrast light panels.
- **Border**: `#334155` (Slate 700)
- **Text Primary**: `#FFFFFF`
- **Text Secondary**: `#CBD5E1` (Slate 300)

## Design Rules

1. **Corners**: Use `rounded-xl` (12px) for cards and `rounded-lg` (8px) for buttons/inputs.
2. **Interactive States**:
   - Focus rings must use the Accent color (`#06B6D4`).
   - Transitions should be `duration-300` or `duration-200`.
3. **Visual Hierarchy**:
   - Use `bg-primary/10` and `text-primary` for subtle tags/badges.
   - Use absolute positioning with `blur-[120px]` and low-opacity accent colors for "glow" effects in the background.
4. **Icons**: Use `lucide-react`. Maintain consistent stroke widths (`strokeWidth={2}`).

## Layout Standards

- **App Shell**: 14rem (64px) sidebar, 3.5rem (14px) header.
- **Typography**: Prioritize `font-bold` for headings and `font-medium` for body text to maintain a professional "tool" feel.
- **Information Density**: Keep cards compact. Use `text-[10px]` or `text-xs` for metadata/labels.
