import { 
  Tags, Home, Car, ShoppingCart, Utensils, 
  Briefcase, HeartPulse, Gamepad2, GraduationCap, 
  Zap, Coffee, Plane, Smartphone, Gift, Wrench
} from 'lucide-react';
import { type ElementType } from 'react';

export const CATEGORY_ICONS: Record<string, ElementType> = {
  tags: Tags,
  home: Home,
  car: Car,
  shopping_cart: ShoppingCart,
  utensils: Utensils,
  briefcase: Briefcase,
  heart_pulse: HeartPulse,
  gamepad2: Gamepad2,
  graduation_cap: GraduationCap,
  zap: Zap,
  coffee: Coffee,
  plane: Plane,
  smartphone: Smartphone,
  gift: Gift,
  wrench: Wrench,
};

export const DEFAULT_ICON_KEY = 'tags';