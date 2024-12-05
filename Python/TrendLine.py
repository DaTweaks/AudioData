import matplotlib.pyplot as plt

# I change the values for the trend line i want to show, chatgpt for the win!
noise_values = [
    0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5,
    1.6, 1.7, 1.8, 1.9, 2.0, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 3.0, 3.1,
    3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 4.0, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7,
    4.8, 4.9, 5.0, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 6.0, 6.1, 6.2, 6.3,
    6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 7
]

percentage_values = [
    100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100,
    100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100,
    100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 99, 99, 98, 99, 98, 98, 95, 90,
    90, 84, 83, 79, 58, 57, 42, 38, 31, 25, 10, 9, 5, 2, 3, 1, 0, 0, 0, 0, 0
]


# Print lengths of the lists
print(len(noise_values))  # Corrected length function
print(len(percentage_values))  # Corrected length function

# Create the plot
plt.figure(figsize=(10, 6))
plt.plot(noise_values, percentage_values, marker='o', color='b')

# Add titles and labels
plt.title('Noise Levels vs FSK Success Rate')
plt.xlabel('Noise Level')
plt.ylabel('Percentage (%)')

# Show grid and the plot
plt.grid(True)
plt.show()

